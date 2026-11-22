using Flux.Transcoder.Ffmpeg.Encoding;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public enum HealthState { Healthy, Stalled, Unknown }

public sealed class SegmentHealthCheck(FfmpegArgumentBuilder argumentBuilder, TimeSpan? staleThreshold = null)
{
    private readonly TimeSpan _staleThreshold = staleThreshold ?? TimeSpan.FromSeconds(15);

    /// <summary>
    /// Здоров, если в каталоге канала есть .ts-сегмент, изменённый недавно.
    /// Если самый свежий сегмент старше порога — источник, скорее всего, упал.
    /// Считаются только сегменты этого запуска: оставшиеся от прошлого не делают канал
    /// ни здоровым, ни зависшим. Ошибки хранилища не перехватываются — это решает вызывающий.
    /// </summary>
    public HealthState Check(Guid channelId, DateTimeOffset startedAt)
    {
        var dir = argumentBuilder.LiveDirectory(channelId);
        if (!Directory.Exists(dir)) return HealthState.Unknown;

        var written = Directory.GetFiles(dir, "*.ts")
            .Select(File.GetLastWriteTimeUtc)
            .Where(time => time >= startedAt.UtcDateTime)
            .ToList();
        if (written.Count == 0) return HealthState.Unknown;

        var newest = written.Max();

        var age = DateTime.UtcNow - newest;
        return age <= _staleThreshold ? HealthState.Healthy : HealthState.Stalled;
    }
}