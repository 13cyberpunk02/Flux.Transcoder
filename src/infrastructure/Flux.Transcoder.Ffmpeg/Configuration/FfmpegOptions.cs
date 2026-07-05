using Flux.Transcoder.Domain.Encoding;

namespace Flux.Transcoder.Ffmpeg.Configuration;

public sealed class FfmpegOptions
{
    public const string SectionName = "Ffmpeg";

    /// <summary>Путь к бинарю ffmpeg (например /usr/bin/ffmpeg).</summary>
    public string BinaryPath { get; set; } = "ffmpeg";

    /// <summary>Корень хранилища сегментов — NAS (общая шара).</summary>
    public string SegmentRoot { get; set; } = @"\\nas\fluxcast";
    
    /// <summary> Продолжительность каждого сегмента записи в секундах </summary>
    public int HlsTimeSeconds { get; set; } = 4;

    /// <summary>Размер live-окна в плейлисте (число сегментов).</summary>
    public int LiveListSize { get; set; } = 6;
}