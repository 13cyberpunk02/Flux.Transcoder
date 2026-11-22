using System.Globalization;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Encoding;
using Flux.Transcoder.Ffmpeg.Configuration;

namespace Flux.Transcoder.Ffmpeg.Encoding;

public sealed class FfmpegArgumentBuilder(FfmpegOptions options)
{
    /// <summary>
    /// Рабочая папка процесса ffmpeg. Все пути в аргументах строятся относительно неё —
    /// так в tee-спецификации нет двоеточия от "D:" (из-за него tee падал с EINVAL).
    /// </summary>
    public string WorkingDirectory => options.SegmentRoot;

    /// <summary>Папка live-плейлиста и его сегментов (короткое скользящее окно).</summary>
    public string LiveDirectory(Guid channelId) =>
        Path.Combine(options.SegmentRoot, channelId.ToString(), "live");

    /// <summary>Корень архива канала (raw-сегменты, разложенные по времени).</summary>
    public string ArchiveDirectory(Guid channelId) =>
        Path.Combine(options.SegmentRoot, channelId.ToString(), "archive");

    public IReadOnlyList<string> Build(Channel channel)
    {
        var rendition = channel.Profile.Renditions
            .OrderByDescending(r => r.Width * r.Height)
            .First();

        Directory.CreateDirectory(LiveDirectory(channel.Id));
        Directory.CreateDirectory(ArchiveDirectory(channel.Id));

        // относительные пути (от WorkingDirectory), всегда через '/'
        var id = channel.Id.ToString();
        var liveSegments    = $"{id}/live/seg_%d.ts";
        var livePlaylist    = $"{id}/live/live.m3u8";
        var archiveSegments = $"{id}/archive/%Y-%m-%d/%H/seg_%Y%m%d_%H%M%S.ts";
        var archivePlaylist = $"{id}/archive/current.m3u8";

        var seg = options.HlsTimeSeconds.ToString(CultureInfo.InvariantCulture);
        var liveListSize = options.LiveListSize.ToString(CultureInfo.InvariantCulture);
        var codec = CodecName(rendition.Codec);

        // Один энкод -> tee -> два HLS-муксера (live + archive).
        // Раньше было два обычных выхода подряд: параметры кодирования (-c:v, -b:v, -vf, -g)
        // в ffmpeg применяются только к СЛЕДУЮЩЕМУ выходу, поэтому архив кодировался
        // второй раз дефолтным libx264 в исходном разрешении.
        var live =
            $"[f=hls:hls_time={seg}:hls_list_size={liveListSize}" +
            ":hls_flags=delete_segments+append_list+independent_segments+omit_endlist" +
            $":hls_segment_filename={liveSegments}]{livePlaylist}";

        var archive =
            $"[f=hls:hls_time={seg}:hls_list_size=6:strftime=1:strftime_mkdir=1" +
            ":hls_flags=append_list+independent_segments+program_date_time" +
            $":hls_segment_filename={archiveSegments}]{archivePlaylist}";

        // NVENC и QSV по -force_key_frames ставят обычный I-кадр, а HLS режет только по IDR:
        // без этого сегменты тянутся до конца GOP (10 сек вместо 4).
        string[] forcedIdr = rendition.Codec switch
        {
            EncoderType.Nvenc => ["-forced-idr", "1"],
            EncoderType.Qsv => ["-forced_idr", "1"],
            _ => []
        };

        return
        [
            "-hide_banner",
            "-loglevel", "info",
            "-i", channel.Source.Value,

            "-map", "0:v:0",
            "-map", "0:a:0?",   // аудио может отсутствовать

            "-vf", $"scale=w={rendition.Width}:h={rendition.Height}",
            "-c:v", codec,
            ..forcedIdr,
            "-b:v", $"{rendition.BitrateKbps}k",
            // ключевой кадр ровно на границе сегмента — сегменты строго по hls_time
            "-force_key_frames", $"expr:gte(t,n_forced*{seg})",
            "-c:a", "aac",
            "-b:a", "128k",

            "-progress", "pipe:1",

            "-f", "tee",
            $"{live}|{archive}"
        ];
    }

    private static string CodecName(EncoderType encoder) => encoder switch
    {
        EncoderType.X264  => "libx264",
        EncoderType.Nvenc => "h264_nvenc",
        EncoderType.Qsv   => "h264_qsv",
        _ => "libx264"
    };
}
