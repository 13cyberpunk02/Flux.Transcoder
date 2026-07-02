using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Domain.Channels;

/// <summary>
/// Политика хранения сегментов канала.
/// HotWindow — сколько держать локально (быстрый доступ, live + недавний архив).
/// ArchiveRetention — сколько хранить в холодном архиве (NAS), до 2 недель, точность до минуты.
/// </summary>
public sealed class RetentionPolicy : ValueObject
{
    public static readonly TimeSpan MaxArchiveRetention = TimeSpan.FromDays(14);
    public static readonly TimeSpan MinArchiveRetention = TimeSpan.FromMinutes(1);

    public TimeSpan HotWindow { get; }
    public TimeSpan ArchiveRetention { get; }

    public RetentionPolicy(TimeSpan hotWindow, TimeSpan archiveRetention)
    {
        // точность до минуты: отбрасываем секунды/миллисекунды
        hotWindow = TruncateToMinutes(hotWindow);
        archiveRetention = TruncateToMinutes(archiveRetention);

        if (archiveRetention < MinArchiveRetention)
            throw new DomainException($"Время хранения архива не может быть меньше {MinArchiveRetention.TotalMinutes} мин.");

        if (archiveRetention > MaxArchiveRetention)
            throw new DomainException($"Время хранения архива не может превышать {MaxArchiveRetention.TotalDays} дней.");

        if (hotWindow < TimeSpan.FromMinutes(1))
            throw new DomainException("Горячее окно не может быть меньше 1 минуты.");

        if (hotWindow > archiveRetention)
            throw new DomainException("Горячее окно не может быть больше времени хранения архива.");

        HotWindow = hotWindow;
        ArchiveRetention = archiveRetention;
    }

    /// <summary>Значение по умолчанию: 3 часа локально, 7 дней в архиве.</summary>
    public static RetentionPolicy Default =>
        new(TimeSpan.FromHours(3), TimeSpan.FromDays(7));

    private static TimeSpan TruncateToMinutes(TimeSpan value) =>
        TimeSpan.FromMinutes(Math.Floor(value.TotalMinutes));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HotWindow;
        yield return ArchiveRetention;
    }
}