using System.Globalization;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Encoding;
using Flux.Transcoder.Ffmpeg.Configuration;

namespace Flux.Transcoder.Ffmpeg.Encoding;

public sealed class FfmpegArgumentBuilder(FfmpegOptions options)
{
    /// <summary>Каталог сегментов для конкретного канала.</summary>
    public string SegmentDirectory(Guid channelId) =>
        Path.Combine(options.SegmentRoot, channelId.ToString("N"));

    public IReadOnlyList<string> Build(Channel channel)
    {
        var renditions = channel.Profile.Renditions.ToList();
        if (renditions.Count == 0)
            throw new InvalidOperationException("Profile has no renditions.");

        var dir = SegmentDirectory(channel.Id);
        Directory.CreateDirectory(dir);

        var args = new List<string> { "-hide_banner", "-loglevel", "info" };

        // вход
        args.Add("-i");
        args.Add(channel.Source.Value);

        // split на N веток
        var split = string.Join("", Enumerable.Range(0, renditions.Count).Select(i => $"[v{i}]"));
        var scales = string.Join("; ", renditions.Select((r, i) =>
            $"[v{i}]scale=w={r.Width}:h={r.Height}[out{i}]"));
        args.Add("-filter_complex");
        args.Add($"[0:v]split={renditions.Count}{split}; {scales}");

        // выходные ветки
        for (var i = 0; i < renditions.Count; i++)
        {
            var r = renditions[i];
            args.Add("-map"); args.Add($"[out{i}]");
            args.Add("-map"); args.Add("0:a?");        // ? = аудио опционально
            args.Add($"-c:v:{i}"); args.Add(CodecName(r.Codec));
            args.Add($"-b:v:{i}"); args.Add($"{r.BitrateKbps}k");
            args.Add($"-c:a:{i}"); args.Add("aac");
            args.Add($"-b:a:{i}"); args.Add("128k");
        }

        var varMap = string.Join(" ",
            Enumerable.Range(0, renditions.Count).Select(i => $"v:{i},a:{i}"));

        args.AddRange([
            "-f", "hls",
            "-hls_time", options.HlsTimeSeconds.ToString(CultureInfo.InvariantCulture),
            "-hls_list_size", options.HlsListSize.ToString(CultureInfo.InvariantCulture),
            "-hls_flags", "delete_segments+independent_segments",
            "-master_pl_name", "master.m3u8",
            "-var_stream_map", varMap,
            "-hls_segment_filename", Path.Combine(dir, "stream_%v_%03d.ts"),
            Path.Combine(dir, "stream_%v.m3u8"),
            "-progress", "pipe:1"
        ]);

        return args;
    }

    private static string CodecName(EncoderType encoder) => encoder switch
    {
        EncoderType.X264 => "libx264",
        EncoderType.Nvenc => "h264_nvenc",
        EncoderType.Qsv => "h264_qsv",
        _ => "libx264"
    };
}