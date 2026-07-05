using Flux.Transcoder.Ffmpeg.Encoding;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public enum HealthState { Healthy, Stalled, Unknown }

public sealed class SegmentHealthCheck(FfmpegArgumentBuilder argumentBuilder, TimeSpan? staleThreshold = null)
{
    private readonly TimeSpan _staleThreshold = staleThreshold ?? TimeSpan.FromSeconds(15);

    /// <summary>
    /// Здоров, если в каталоге канала есть .ts-сегмент, изменённый недавно.
    /// Если самый свежий сегмент старше порога — источник, скорее всего, упал.
    /// </summary>
    public HealthState Check(Guid channelId)
    {
        var dir = argumentBuilder.LiveDirectory(channelId);
        if (!Directory.Exists(dir)) return HealthState.Unknown;

        var segments = Directory.GetFiles(dir, "*.ts");
        if (segments.Length == 0) return HealthState.Unknown;

        var newest = segments
            .Select(File.GetLastWriteTimeUtc)
            .Max();

        var age = DateTime.UtcNow - newest;
        return age <= _staleThreshold ? HealthState.Healthy : HealthState.Stalled;
    }
}