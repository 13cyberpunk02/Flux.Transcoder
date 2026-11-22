using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

/// <summary>
/// Помнит попытки перезапуска текущей ревизии каждого канала: когда пробовать снова и не пора ли сдаваться.
/// Сам ничего не запускает и в БД не ходит — это делает реконсилер воркера.
/// </summary>
public sealed class RestartCoordinator(RestartPolicy policy, ILogger<RestartCoordinator> logger)
{
    private static readonly TimeSpan StorageRetryDelay = TimeSpan.FromSeconds(10);

    private sealed class Run(int revision)
    {
        public int Revision { get; } = revision;
        public int Attempts { get; set; }
        public DateTimeOffset? RetryAt { get; set; }
        public bool Exhausted { get; set; }
    }

    private readonly Lock _sync = new();
    private readonly Dictionary<Guid, Run> _runs = [];

    /// <summary>Канал должен работать с этой ревизией. Новая ревизия (действие пользователя) — попытки с нуля.</summary>
    public void Track(Guid channelId, int revision)
    {
        lock (_sync)
        {
            if (!_runs.TryGetValue(channelId, out var run) || run.Revision != revision)
                _runs[channelId] = new Run(revision);
        }
    }

    /// <summary>Канал больше не должен работать.</summary>
    public void Forget(Guid channelId)
    {
        lock (_sync) _runs.Remove(channelId);
    }

    public bool IsExhausted(Guid channelId)
    {
        lock (_sync) return _runs.TryGetValue(channelId, out var run) && run.Exhausted;
    }

    /// <summary>true — пауза перед попыткой прошла, можно запускать (и попытка этим занята).</summary>
    public bool TryBeginStart(Guid channelId)
    {
        lock (_sync)
        {
            if (!_runs.TryGetValue(channelId, out var run) || run.Exhausted || run.RetryAt > DateTimeOffset.UtcNow)
                return false;

            run.RetryAt = null;
            return true;
        }
    }

    /// <summary>Процесс этой ревизии упал, завис, не дал первого сегмента или потерял источник.</summary>
    public void OnFailure(Guid channelId, int revision)
    {
        lock (_sync)
        {
            // RetryAt уже стоит — это тот же сбой (reaper и Exited сообщают о нём оба)
            if (!_runs.TryGetValue(channelId, out var run) || run.Revision != revision
                || run.Exhausted || run.RetryAt is not null)
                return;

            if (!policy.CanRetry(run.Attempts))
            {
                run.Exhausted = true;
                logger.LogError(
                    "Канал {ChannelId}: исчерпан лимит перезапусков ({Attempt}). Останавливаю.",
                    channelId, run.Attempts);
                return;
            }

            var delay = policy.DelayFor(run.Attempts);
            run.Attempts++;
            run.RetryAt = DateTimeOffset.UtcNow + delay;
            logger.LogWarning(
                "Канал {ChannelId} требует перезапуска через {Delay:g} (попытка {Next}).",
                channelId, delay, run.Attempts);
        }
    }

    /// <summary>
    /// ffmpeg не удалось даже запустить. Недоступное хранилище — не вина канала:
    /// ждём его, не тратя попыток, иначе сбой NAS на 6 минут перевёл бы все каналы в Failed.
    /// </summary>
    public void OnLaunchFailed(Guid channelId, int revision, Exception error)
    {
        if (error is not (IOException or UnauthorizedAccessException))
        {
            logger.LogError(error, "Не удалось запустить ffmpeg канала {ChannelId}", channelId);
            OnFailure(channelId, revision);
            return;
        }

        lock (_sync)
        {
            if (!_runs.TryGetValue(channelId, out var run) || run.Revision != revision || run.Exhausted)
                return;

            run.RetryAt = DateTimeOffset.UtcNow + StorageRetryDelay;
        }

        logger.LogWarning(
            "Канал {ChannelId}: хранилище недоступно ({Reason}), повтор через {Delay:g}.",
            channelId, error.Message, StorageRetryDelay);
    }

    /// <summary>Сбросить счётчик, когда канал снова здоров.</summary>
    public void OnHealthy(Guid channelId, int revision)
    {
        lock (_sync)
        {
            if (_runs.TryGetValue(channelId, out var run) && run.Revision == revision)
                run.Attempts = 0;
        }
    }

    /// <summary>Каналы, у которых пауза перед перезапуском закончилась.</summary>
    public IReadOnlyCollection<Guid> Due()
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            return _runs.Where(x => !x.Value.Exhausted && x.Value.RetryAt <= now).Select(x => x.Key).ToList();
        }
    }

    public IReadOnlyCollection<Guid> Tracked()
    {
        lock (_sync) return _runs.Keys.ToList();
    }
}
