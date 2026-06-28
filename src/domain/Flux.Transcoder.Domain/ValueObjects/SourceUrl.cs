using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Domain.ValueObjects;

public sealed class SourceUrl : ValueObject
{
    private static readonly string[] AllowedSchemes = { "srt://", "rtmp://", "udp://" };

    public string Value { get; }

    public SourceUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Ссылка \"URL\" не может быть пустым.");

        var trimmed = value.Trim();

        if (!AllowedSchemes.Any(s => trimmed.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException(
                $"Ссылка \"URL\" неправильно заполнена '{value}'. Доступные форматы: srt://, rtmp://, udp://.");

        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}