using System.Collections.Concurrent;
using Flux.Transcoder.Ffmpeg.Encoding;

namespace Flux.Transcoder.Ffmpeg.Runtime;

/// <summary>
/// Измеряет реальный выходной битрейт канала по приросту размера
/// записанных HLS-сегментов (.ts) за интервал. Устойчив к удалению
/// старых сегментов, так как считает дельту по новым/изменённым файлам.
/// </summary>
public sealed class BitrateMeter(FfmpegArgumentBuilder argumentBuilder)
{
    // на канал: последний зафиксированный write-time сегментов + момент замера
    private sealed record State(DateTime LastSegmentWriteUtc, DateTimeOffset MeasuredAt);
    private readonly ConcurrentDictionary<Guid, State> _states = new();

    /// <summary>
    /// Возвращает измеренный выходной битрейт в кбит/с.
    /// Первый вызов после старта вернёт 0 (нужна база для дельты).
    /// </summary>
    public double MeasureKbps(Guid channelId)
    {
        var dir = argumentBuilder.SegmentDirectory(channelId);
        if (!Directory.Exists(dir))
            return 0;

        var now = DateTimeOffset.UtcNow;

        var segments = Directory.GetFiles(dir, "*.ts")
            .Select(f => new FileInfo(f))
            .Where(fi => fi.Exists)
            .ToList();

        if (segments.Count == 0)
            return 0;

        _states.TryGetValue(channelId, out var prev);

        // граница: учитываем сегменты, записанные ПОСЛЕ предыдущего замера
        var since = prev?.LastSegmentWriteUtc ?? DateTime.MinValue;

        long newBytes = segments
            .Where(fi => fi.LastWriteTimeUtc > since)
            .Sum(fi => fi.Length);

        var newestWrite = segments.Max(fi => fi.LastWriteTimeUtc);

        double kbps = 0;
        if (prev is not null)
        {
            var deltaSec = (now - prev.MeasuredAt).TotalSeconds;
            if (deltaSec > 0 && newBytes > 0)
                kbps = newBytes * 8.0 / 1000.0 / deltaSec; // байты→биты→кило, делим на секунды
        }

        _states[channelId] = new State(newestWrite, now);
        return Math.Round(kbps, 1);
    }

    public void Reset(Guid channelId) => _states.TryRemove(channelId, out _);
}