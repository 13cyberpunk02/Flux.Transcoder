namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class RestartPolicy(
    int maxAttempts = 8,
    TimeSpan? baseDelay = null,
    TimeSpan? maxDelay = null)
{
    private readonly TimeSpan _baseDelay = baseDelay ?? TimeSpan.FromSeconds(2);
    private readonly TimeSpan _maxDelay = maxDelay ?? TimeSpan.FromMinutes(2);

    /// <summary>Можно ли ещё пытаться.</summary>
    public bool CanRetry(int attempt) => attempt < maxAttempts;

    /// <summary>Задержка перед попыткой #attempt (0-based) с экспоненциальным backoff + джиттер.</summary>
    public TimeSpan DelayFor(int attempt)
    {
        var exp = _baseDelay.TotalSeconds * Math.Pow(2, attempt);
        var capped = Math.Min(exp, _maxDelay.TotalSeconds);

        // джиттер ±20%, чтобы каналы не перезапускались синхронно (thundering herd)
        var jitter = Random.Shared.NextDouble() * 0.4 - 0.2;
        var seconds = capped * (1 + jitter);

        return TimeSpan.FromSeconds(Math.Max(_baseDelay.TotalSeconds, seconds));
    }
}