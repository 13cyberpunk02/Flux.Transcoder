using System.Globalization;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Encoding;
using Flux.Transcoder.Ffmpeg.Configuration;

namespace Flux.Transcoder.Ffmpeg.Encoding;

public sealed class FfmpegArgumentBuilder(FfmpegOptions options)
{
    /// <summary>Папка live-плейлиста и его сегментов (горячее, короткое окно).</summary>
    public string LiveDirectory(Guid channelId) =>
        Path.Combine(options.HotRoot, channelId.ToString(), "live");

    /// <summary>Корень архива канала в горячем хранилище (raw-сегменты по времени).</summary>
    public string ArchiveHotRoot(Guid channelId) =>
        Path.Combine(options.HotRoot, channelId.ToString(), "archive");

    /// <summary>Корень архива канала в холодном хранилище (NAS).</summary>
    public string ArchiveColdRoot(Guid channelId) =>
        Path.Combine(options.ColdRoot, channelId.ToString(), "archive");

    public IReadOnlyList<string> Build(Channel channel)
    {
        // берём старший рендишен профиля как единственное качество архива+live
        var rendition = channel.Profile.Renditions
            .OrderByDescending(r => r.Width * r.Height)
            .First();

        var liveDir = LiveDirectory(channel.Id);
        var archiveDir = ArchiveHotRoot(channel.Id);
        Directory.CreateDirectory(liveDir);
        Directory.CreateDirectory(archiveDir);

        // ffmpeg-пути только с прямым слэшем — кроссплатформенно
        var liveSegments   = ToFfmpegPath(Path.Combine(liveDir, "seg_%d.ts"));
        var livePlaylist   = ToFfmpegPath(Path.Combine(liveDir, "live.m3u8"));

        // архив: папки YYYY-MM-DD/HH/, время в имени файла (точность до секунды)
        var archiveSegments = ToFfmpegPath(
            Path.Combine(archiveDir, "%Y-%m-%d", "%H", "seg_%Y%m%d_%H%M%S.ts"));
        // технический архивный плейлист (короткий, нужен ffmpeg формально; реальные плейлисты строим сами)
        var archivePlaylist = ToFfmpegPath(Path.Combine(archiveDir, "current.m3u8"));

        var seg = options.HlsTimeSeconds.ToString(CultureInfo.InvariantCulture);
        var codec = CodecName(rendition.Codec);

        var args = new List<string>
        {
            "-hide_banner",
            "-loglevel", "info",

            // вход
            "-i", channel.Source.Value,

            // масштаб до целевого качества (одно качество)
            "-vf", $"scale=w={rendition.Width}:h={rendition.Height}",

            // одно кодирование
            "-c:v", codec,
            "-b:v", $"{rendition.BitrateKbps}k",
            "-c:a", "aac",
            "-b:a", "128k",

            // ключевые кадры выровнены под длину сегмента (важно для HLS)
            "-g", (rendition is { } ? "48" : "48"),   // ~2s при 24fps; можно вычислять от fps

            // прогресс
            "-progress", "pipe:1",

            // tee: один энкод -> два HLS-выхода
            "-f", "tee",
            BuildTeeSpec(seg, liveSegments, livePlaylist, archiveSegments, archivePlaylist)
        };

        return args;
    }

    private string BuildTeeSpec(
        string seg,
        string liveSegments, string livePlaylist,
        string archiveSegments, string archivePlaylist)
    {
        // ВЫХОД 1 — LIVE: короткое скользящее окно, удаляет старое само
        var live =
            $"[f=hls:" +
            $"hls_time={seg}:" +
            $"hls_list_size={options.LiveListSize}:" +
            $"hls_flags=delete_segments+append_list+independent_segments:" +
            $"hls_segment_filename={liveSegments}]" +
            $"{livePlaylist}";

        // ВЫХОД 2 — АРХИВ: strftime-раскладка по времени, БЕЗ delete_segments (чистит наш retention)
        var archive =
            $"[f=hls:" +
            $"hls_time={seg}:" +
            $"hls_list_size=6:" +
            $"strftime=1:" +
            $"strftime_mkdir=1:" +
            $"hls_flags=append_list+independent_segments+program_date_time:" +
            $"hls_segment_filename={archiveSegments}]" +
            $"{archivePlaylist}";

        // onfail=ignore на live — чтобы сбой live не ронял архив (архив важнее)
        return $"{live}|{archive}";
    }

    private static string CodecName(EncoderType encoder) => encoder switch
    {
        EncoderType.X264  => "libx264",
        EncoderType.Nvenc => "h264_nvenc",
        EncoderType.Qsv   => "h264_qsv",
        _ => "libx264"
    };

    // Windows-путь -> ffmpeg-совместимый (прямые слэши)
    private static string ToFfmpegPath(string path) => path.Replace('\\', '/');
}