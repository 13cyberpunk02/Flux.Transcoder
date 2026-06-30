namespace Flux.Transcoder.Application.Profiles.Queries.GetAllProfiles;

public sealed record ProfileView(Guid ProfileId, string Name, IReadOnlyList<RenditionView> Renditions);