namespace Flux.Transcoder.Application.Profiles.Queries.GetAllProfiles;

public sealed record RenditionView(int Width, int Height, int BitrateKbps, string Codec);