namespace Flux.Transcoder.Application.Profiles.Commands.CreateProfile;

public sealed record CreateProfileCommand(string Name, IReadOnlyList<RenditionInput> Renditions);
