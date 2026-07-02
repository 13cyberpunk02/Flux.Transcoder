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
        var rendition = channel.Profile.Renditions
            .OrderByDescending(r => r.Width * r.Height)
            .First();

        var liveDir = LiveDirectory(channel.Id);
        var archiveDir = ArchiveHotRoot(channel.Id);
        Directory.CreateDirectory(liveDir);
        Directory.CreateDirectory(archiveDir);

        var liveSegments   = ToFfmpegPath(Path.Combine(liveDir, "seg_%d.ts"));
        var livePlaylist   = ToFfmpegPath(Path.Combine(liveDir, "live.m3u8"));
        var archiveSegments = ToFfmpegPath(
            Path.Combine(archiveDir, "%Y-%m-%d", "%H", "seg_%Y%m%d_%H%M%S.ts"));
        var archivePlaylist = ToFfmpegPath(Path.Combine(archiveDir, "current.m3u8"));

        var seg = options.HlsTimeSeconds.ToString(CultureInfo.InvariantCulture);
        var codec = CodecName(rendition.Codec);

        var args = new List<string>
        {
            "-hide_banner",
            "-loglevel", "info",
            "-i", channel.Source.Value,

            "-vf", $"scale=w={rendition.Width}:h={rendition.Height}",
            "-c:v", codec,
            "-b:v", $"{rendition.BitrateKbps}k",
            "-c:a", "aac",
            "-b:a", "128k",
            "-g", "48",

            "-progress", "pipe:1",

            // ВЫХОД 1 — LIVE
            "-f", "hls",
            "-hls_time", seg,
            "-hls_list_size", options.LiveListSize.ToString(CultureInfo.InvariantCulture),
            "-hls_flags", "delete_segments+append_list+independent_segments+omit_endlist",
            "-hls_segment_filename", liveSegments,
            livePlaylist,

            // ВЫХОД 2 — АРХИВ
            "-f", "hls",
            "-hls_time", seg,
            "-hls_list_size", "6",
            "-strftime", "1",
            "-strftime_mkdir", "1",
            "-hls_flags", "append_list+independent_segments+program_date_time",
            "-hls_segment_filename", archiveSegments,
            archivePlaylist
        };

        return args;
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