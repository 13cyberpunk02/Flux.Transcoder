using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Domain.Channels;

/// <summary>
/// Политика хранения сегментов канала.
/// ArchiveRetention — сколько хранить в архиве (NAS), до 2 недель, точность до минуты.
/// </summary>
public sealed class RetentionPolicy : ValueObject
{
    public static readonly TimeSpan MaxRetention = TimeSpan.FromDays(14);
    public static readonly TimeSpan MinRetention = TimeSpan.FromMinutes(1);

    public TimeSpan ArchiveRetention { get; }

    public RetentionPolicy(TimeSpan archiveRetention)
    {
        archiveRetention = TruncateToMinutes(archiveRetention);

        if (archiveRetention < MinRetention)
            throw new DomainException($"Время хранения архива не может быть меньше {MinRetention.TotalMinutes} мин.");
        if (archiveRetention > MaxRetention)
            throw new DomainException($"Время хранения архива не может превышать {MaxRetention.TotalDays} дней.");

        ArchiveRetention = archiveRetention;
    }

    public static RetentionPolicy Default => new(TimeSpan.FromDays(7));

    private static TimeSpan TruncateToMinutes(TimeSpan value) =>
        TimeSpan.FromMinutes(Math.Floor(value.TotalMinutes));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ArchiveRetention;
    }
}