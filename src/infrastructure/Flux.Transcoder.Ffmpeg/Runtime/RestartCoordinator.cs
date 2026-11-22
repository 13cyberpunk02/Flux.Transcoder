using System.Collections.Concurrent;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class RestartCoordinator(
    RestartPolicy policy,
    IServiceProvider services,
    ILogger<RestartCoordinator> logger)
{
    // счётчик попыток и отложенный перезапуск на канал
    private readonly ConcurrentDictionary<Guid, int> _attempts = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _pending = new();
    // перезапуски, дошедшие до StartAsync: ещё отменяемы, но уже не мешают запланировать следующий
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _starting = new();

    /// <summary>
    /// Канал завис или упал — планирует перезапуск. Процесс здесь не останавливается:
    /// это делает вызывающий, он знает конкретный экземпляр (по channelId можно убить уже новый).
    /// </summary>
    public void OnStalled(Guid channelId)
    {
        var pending = new CancellationTokenSource();
        if (!_pending.TryAdd(channelId, pending))
            return;

        var attempt = _attempts.GetOrAdd(channelId, 0);

        if (!policy.CanRetry(attempt))
        {
            logger.LogError(
                "Канал {ChannelId}: исчерпан лимит перезапусков ({Attempt}). Останавливаю.",
                channelId, attempt);
            _ = MarkFailedAsync(channelId);
            _pending.TryRemove(new(channelId, pending));
            return;
        }

        var delay = policy.DelayFor(attempt);
        logger.LogWarning(
            "Канал {ChannelId} требует перезапуска через {Delay:g} (попытка {Next}).",
            channelId, delay, attempt + 1);

        _ = ScheduleRestartAsync(channelId, delay, pending);
    }

    /// <summary>Сбросить счётчик, когда канал снова здоров.</summary>
    public void OnHealthy(Guid channelId) => _attempts.TryRemove(channelId, out _);

    /// <summary>Ручной старт/стоп/рестарт: сбросить счётчик и отменить отложенный перезапуск.</summary>
    public void Reset(Guid channelId)
    {
        _attempts.TryRemove(channelId, out _);
        if (_pending.TryRemove(channelId, out var pending))
            pending.Cancel();
        if (_starting.TryRemove(channelId, out var starting))
            starting.Cancel();
    }

    private async Task ScheduleRestartAsync(Guid channelId, TimeSpan delay, CancellationTokenSource pending)
    {
        var token = pending.Token;
        try
        {
            await Task.Delay(delay, token);

            Channel? channel;
            IStreamOrchestrator orchestrator;

            await using (var scope = services.CreateAsyncScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IReadRepository<Channel>>();
                channel = await repo.FirstOrDefaultAsync(new ChannelByIdSpec(channelId), token);

                // оркестратор достаём ЗДЕСЬ, а не в конструкторе — это разрывает круг
                orchestrator = scope.ServiceProvider.GetRequiredService<IStreamOrchestrator>();
            }

            if (channel is null)
            {
                logger.LogWarning("Канал {ChannelId} не найден при перезапуске.", channelId);
                return;
            }

            // пока ждали backoff, пользователь мог остановить канал — не воскрешаем его
            if (channel.Status is not (ChannelStatus.Starting or ChannelStatus.Running or ChannelStatus.Degraded))
            {
                logger.LogInformation(
                    "Канал {ChannelId} в статусе {Status} — авто-перезапуск отменён.",
                    channelId, channel.Status);
                return;
            }

            // новый процесс может упасть сразу — его перезапуск не должен упереться в этот
            _starting[channelId] = pending;
            _pending.TryRemove(new(channelId, pending));
            token.ThrowIfCancellationRequested();
            _attempts.AddOrUpdate(channelId, 1, (_, cur) => cur + 1);
            // токен перепроверит оркестратор под блокировкой: Stop мог прийти, пока читали БД
            await orchestrator.StartAsync(channel, token);

            logger.LogInformation("Канал {ChannelId} перезапущен.", channelId);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            logger.LogInformation("Канал {ChannelId}: авто-перезапуск отменён пользователем.", channelId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при перезапуске канала {ChannelId}", channelId);
        }
        finally
        {
            _pending.TryRemove(new(channelId, pending));
            _starting.TryRemove(new(channelId, pending));
        }
    }

    private async Task MarkFailedAsync(Guid channelId)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Channel>>();
            
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var channel = await repo.FirstOrDefaultAsync(new ChannelByIdSpec(channelId));
            if (channel is { Status: ChannelStatus.Starting or ChannelStatus.Running or ChannelStatus.Degraded })
            {
                channel.MarkFailed("Источник недоступен, исчерпан лимит перезапусков.");
                await uow.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось пометить канал {ChannelId} как Failed", channelId);
        }
    }
}