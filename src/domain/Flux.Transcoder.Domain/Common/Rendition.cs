using Flux.Transcoder.Domain.Encoding;

namespace Flux.Transcoder.Domain.Common;

public sealed class Rendition : ValueObject
{
    public int Width { get; }
    public int Height { get; }
    public int BitrateKbps { get; }
    public EncoderType Codec { get; }

    public Rendition(int width, int height, int bitrateKbps, EncoderType codec)
    {
        if (width <= 0 || height <= 0)
            throw new DomainException("Rendition resolution must be positive.");
        if (bitrateKbps <= 0)
            throw new DomainException("Rendition bitrate must be positive.");

        Width = width;
        Height = height;
        BitrateKbps = bitrateKbps;
        Codec = codec;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Width;
        yield return Height;
        yield return BitrateKbps;
        yield return Codec;
    }

    public override string ToString() => $"{Width}x{Height}@{BitrateKbps}kbps ({Codec})";
}