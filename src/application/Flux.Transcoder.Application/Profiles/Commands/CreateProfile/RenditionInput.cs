using Flux.Transcoder.Domain.Encoding;

namespace Flux.Transcoder.Application.Profiles.Commands.CreateProfile;

public sealed record RenditionInput(int Width, int Height, int BitrateKbps, EncoderType Codec);