using Flux.Transcoder.Domain.Encoding;

namespace Flux.Transcoder.Ffmpeg.Configuration;

public sealed class FfmpegOptions
{
    public const string SectionName = "Ffmpeg";

    /// <summary>Путь к бинарю ffmpeg (например /usr/bin/ffmpeg).</summary>
    public string BinaryPath { get; set; } = "ffmpeg";

    /// <summary>Каталог в tmpfs, куда пишутся сегменты HLS (RAM-диск).</summary>
    public string SegmentRoot { get; set; } = "/dev/shm/broadcast";

    /// <summary>Дефолтный энкодер, если в рендишене не задан явно.</summary>
    public EncoderType DefaultEncoder { get; set; } = EncoderType.X264;

    /// <summary>Длина сегмента HLS в секундах.</summary>
    public int HlsTimeSeconds { get; set; } = 4;

    /// <summary>Размер плейлиста (скользящее окно). 0 = event-плейлист без обрезки.</summary>
    public int HlsListSize { get; set; } = 6;
}