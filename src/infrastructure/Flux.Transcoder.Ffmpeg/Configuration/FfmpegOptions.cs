using Flux.Transcoder.Domain.Encoding;

namespace Flux.Transcoder.Ffmpeg.Configuration;

public sealed class FfmpegOptions
{
    public const string SectionName = "Ffmpeg";

    /// <summary>Путь к бинарю ffmpeg (например /usr/bin/ffmpeg).</summary>
    public string BinaryPath { get; set; } = "ffmpeg";

    /// <summary>Горячее хранилище: быстрый локальный диск. Сюда пишет ffmpeg (live + свежий архив).</summary>
    public string HotRoot { get; set; } = @"D:\fluxcast\hot";

    /// <summary>Холодное хранилище: NAS. Сюда переносятся остывшие архивные сегменты.</summary>
    public string ColdRoot { get; set; } = @"D:\fluxcast\cold";

    public int HlsTimeSeconds { get; set; } = 4;

    /// <summary>Размер live-окна в плейлисте (число сегментов).</summary>
    public int LiveListSize { get; set; } = 6;
}