using System.Collections.Concurrent;
using System.Globalization;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class ProgressParser
{
    private readonly ConcurrentDictionary<Guid, Dictionary<string, string>> _buffers = new();
    private readonly ConcurrentDictionary<Guid, ProgressSnapshot> _latest = new();

    /// <summary>Скармливаем строку stdout от ffmpeg.</summary>
    public void Feed(Guid channelId, string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        var idx = line.IndexOf('=');
        if (idx <= 0) return;

        var key = line[..idx].Trim();
        var value = line[(idx + 1)..].Trim();

        var buffer = _buffers.GetOrAdd(channelId, _ => new Dictionary<string, string>());

        lock (buffer)
        {
            buffer[key] = value;

            // конец блока прогресса
            if (key == "progress")
            {
                _latest[channelId] = BuildSnapshot(channelId, buffer);
                buffer.Clear();
            }
        }
    }

    public ProgressSnapshot? Latest(Guid channelId) =>
        _latest.TryGetValue(channelId, out var s) ? s : null;

    public void Reset(Guid channelId)
    {
        _buffers.TryRemove(channelId, out _);
        _latest.TryRemove(channelId, out _);
    }

    private static ProgressSnapshot BuildSnapshot(Guid channelId, Dictionary<string, string> b)
    {
        long frame = ParseLong(b, "frame");
        double fps = ParseDouble(b, "fps");
        double bitrate = ParseBitrate(b); // "bitrate" в kbits/s, может быть "N/A"
        long outTimeMs = ParseLong(b, "out_time_ms"); // микросекунды(!) несмотря на имя
        long dropped = ParseLong(b, "drop_frames");

        return new ProgressSnapshot(
            channelId,
            frame,
            fps,
            bitrate,
            TimeSpan.FromMilliseconds(outTimeMs / 1000.0),
            dropped,
            DateTimeOffset.UtcNow);
    }

    private static long ParseLong(Dictionary<string, string> b, string key) =>
        b.TryGetValue(key, out var v) && long.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var r)
            ? r
            : 0;

    private static double ParseDouble(Dictionary<string, string> b, string key) =>
        b.TryGetValue(key, out var v) && double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var r)
            ? r
            : 0;

    private static double ParseBitrate(Dictionary<string, string> b)
    {
        if (!b.TryGetValue("bitrate", out var v)) return 0;
        v = v.Replace("kbits/s", "").Trim();
        return double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0;
    }
}