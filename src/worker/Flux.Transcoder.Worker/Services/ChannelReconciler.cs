using System.Collections.Concurrent;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Contracts.Events;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Ffmpeg.Runtime;
using Wolverine.Runtime;

namespace Flux.Transcoder.Worker.Services;

/// <summary>
/// Приводит процессы ffmpeg к состоянию в БД: активный канал — один процесс его RunRevision,
/// неактивный — ни одного.
/// Команды из очереди лишь ускоряют сверку, поэтому их потеря, дубль или перестановка не страшны,
/// а после рестарта воркера каналы поднимаются сами.
/// </summary>
public sealed class ChannelReconciler(
    IServiceProvider services,
    IStreamOrchestrator orchestrator,
    ProcessSupervisor supervisor,
    RestartCoordinator restartCoordinator,
    IWolverineRuntime runtime,
    ILogger<ChannelReconciler> logger)
    : BackgroundService
{
    private const string FailureReason = "Источник недоступен, исчерпан лимит перезапусков.";

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(10);

    private sealed class Gate
    {
        public readonly SemaphoreSlim Lock = new(1, 1);
        public int Waiting;
    }

    private readonly ConcurrentDictionary<Guid, Gate> _gates = new();

    // при старте со 100 каналами не открываем 100 соединений с БД разом
    private readonly SemaphoreSlim _dbSlots = new(8, 8);

    /// <summary>
    /// Запросить сверку канала и сразу вернуться: очередь команд одна на все каналы,
    /// её не должны задерживать ни БД, ни зависшее хранилище.
    /// </summary>
    public void Trigger(Guid channelId)
    {
        var gate = _gates.GetOrAdd(channelId, _ => new Gate());

        // ожидающая сверка и так прочитает самое свежее состояние
        if (Interlocked.Exchange(ref gate.Waiting, 1) == 1)
            return;

        _ = Task.Run(() => RunAsync(channelId, gate));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextSweep = DateTimeOffset.MinValue;   // первая сверка сразу — это и есть восстановление
        using var timer = new PeriodicTimer(TickInterval);

        try
        {
            do
            {
                foreach (var channelId in restartCoordinator.Due())
                    Trigger(channelId);

                if (DateTimeOffset.UtcNow < nextSweep)
                    continue;

                await SweepAsync(stoppingToken);
                nextSweep = DateTimeOffset.UtcNow + SweepInterval;
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // штатное выключение
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await supervisor.ShutdownAsync();
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        try
        {
            List<Channel> active;
            await using (var scope = services.CreateAsyncScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IReadRepository<Channel>>();
                active = await repository.ListAsync(new ActiveChannelsSpec(), ct);
            }

            // процессы и попытки каналов, которые уже не активны
            var mismatched = supervisor.All().Select(p => p.ChannelId)
                .Concat(restartCoordinator.Tracked())
                .ToHashSet();

            foreach (var channel in active)
            {
                if (supervisor.Get(channel.Id) is { IntentionalStop: false } running
                    && running.Revision == channel.RunRevision)
                    mismatched.Remove(channel.Id);
                else
                    mismatched.Add(channel.Id);
            }

            foreach (var channelId in mismatched)
                Trigger(channelId);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // без БД ничего не останавливаем: эфир важнее сверки
            logger.LogWarning("Сверка с БД не удалась, повторю: {Reason}", ex.Message);
        }
    }

    private async Task RunAsync(Guid channelId, Gate gate)
    {
        await gate.Lock.WaitAsync();
        try
        {
            Volatile.Write(ref gate.Waiting, 0);
            await ReconcileAsync(channelId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сверка канала {ChannelId} не удалась, повторю", channelId);
        }
        finally
        {
            gate.Lock.Release();
        }
    }

    private async Task ReconcileAsync(Guid channelId)
    {
        if (supervisor.IsClosing)
            return;

        Channel? channel;
        var failedFrom = (ChannelStatus?)null;

        await _dbSlots.WaitAsync();
        try
        {
            await using var scope = services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Channel>>();
            channel = await repository.FirstOrDefaultAsync(new ChannelByIdSpec(channelId));

            if (channel is { IsInactive: false })
            {
                restartCoordinator.Track(channelId, channel.RunRevision);

                if (restartCoordinator.IsExhausted(channelId))
                {
                    failedFrom = channel.Status;
                    channel.MarkFailed(FailureReason);
                    // если пользователь успел вмешаться, сохранение упадёт по concurrency token,
                    // а следующая сверка увидит его решение
                    await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
                }
            }
        }
        finally
        {
            _dbSlots.Release();
        }

        if (channel is null || channel.IsInactive)
        {
            restartCoordinator.Forget(channelId);
            await orchestrator.StopAsync(channelId);

            if (channel is not null && failedFrom is { } previous)
                await PublishFailedAsync(channel, previous);

            return;
        }

        if (supervisor.Get(channelId) is { IntentionalStop: false } running && running.Revision == channel.RunRevision)
            return;

        if (!restartCoordinator.TryBeginStart(channelId))
            return;   // ждём окончания паузы

        try
        {
            await orchestrator.StartAsync(channel);
        }
        catch (Exception ex)
        {
            restartCoordinator.OnLaunchFailed(channelId, channel.RunRevision, ex);
        }
    }

    private async Task PublishFailedAsync(Channel channel, ChannelStatus previous)
    {
        try
        {
            await new MessageBus(runtime).PublishAsync(new ChannelStateChanged(
                channel.Id, previous.ToString(), channel.Status.ToString(), DateTimeOffset.UtcNow, FailureReason));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сообщить о Failed канала {ChannelId}", channel.Id);
        }
    }
}
