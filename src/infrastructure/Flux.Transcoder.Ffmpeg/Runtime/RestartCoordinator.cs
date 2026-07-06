using System.Collections.Concurrent;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class RestartCoordinator(
    ProcessSupervisor supervisor,
    RestartPolicy policy,
    IServiceProvider services,
    ILogger<RestartCoordinator> logger)
{
    // счётчик попыток и флаг «перезапуск уже запланирован» на канал
    private readonly ConcurrentDictionary<Guid, int> _attempts = new();
    private readonly ConcurrentDictionary<Guid, bool> _restarting = new();

    /// <summary>Вызывается reaper'ом, когда канал завис (Stalled).</summary>
    public void OnStalled(Guid channelId)
    {
        if (!_restarting.TryAdd(channelId, true))
            return;

        var attempt = _attempts.GetOrAdd(channelId, 0);

        if (!policy.CanRetry(attempt))
        {
            logger.LogError(
                "Канал {ChannelId}: исчерпан лимит перезапусков ({Attempt}). Останавливаю.",
                channelId, attempt);
            supervisor.Stop(channelId);
            _ = MarkFailedAsync(channelId);
            _restarting.TryRemove(channelId, out _);
            return;
        }

        var delay = policy.DelayFor(attempt);
        logger.LogWarning(
            "Канал {ChannelId} требует перезапуска через {Delay:g} (попытка {Next}).",
            channelId, delay, attempt + 1);

        _ = ScheduleRestartAsync(channelId, delay);
    }

    /// <summary>Сбросить счётчик, когда канал снова здоров.</summary>
    public void OnHealthy(Guid channelId) => _attempts.TryRemove(channelId, out _);

    private async Task ScheduleRestartAsync(Guid channelId, TimeSpan delay)
    {
        try
        {
            supervisor.Stop(channelId);
            await Task.Delay(delay);

            Channel? channel;
            IStreamOrchestrator orchestrator;

            await using (var scope = services.CreateAsyncScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IReadRepository<Channel>>();
                channel = await repo.FirstOrDefaultAsync(new ChannelByIdSpec(channelId));

                // оркестратор достаём ЗДЕСЬ, а не в конструкторе — это разрывает круг
                orchestrator = scope.ServiceProvider.GetRequiredService<IStreamOrchestrator>();
            }

            if (channel is null)
            {
                logger.LogWarning("Канал {ChannelId} не найден при перезапуске.", channelId);
                return;
            }

            _attempts.AddOrUpdate(channelId, 1, (_, cur) => cur + 1);
            await orchestrator.StartAsync(channel);

            logger.LogInformation("Канал {ChannelId} перезапущен.", channelId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при перезапуске канала {ChannelId}", channelId);
        }
        finally
        {
            _restarting.TryRemove(channelId, out _);
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
            if (channel is not null && channel.Status is not ChannelStatus.Failed)
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