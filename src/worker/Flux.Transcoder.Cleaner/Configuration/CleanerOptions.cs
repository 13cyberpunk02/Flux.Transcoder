namespace Flux.Transcoder.Cleaner.Configuration;

public sealed class CleanerOptions
{
    public const string SectionName = "Cleaner";

    /// <summary>Корень архива — общее хранилище, куда пишут все ffmpeg-воркеры (NAS в проде).</summary>
    public string ArchiveRoot { get; set; } = @"D:\fluxcast\hot";

    /// <summary>Как часто проверять (минимум минута — точность retention до минуты).</summary>
    public int IntervalMinutes { get; set; } = 1;
}