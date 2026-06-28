using Flux.Transcoder.Application.Profiles.Commands.CreateProfile;
using Flux.Transcoder.Domain.Encoding;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

public sealed record RenditionDto(int Width, int Height, int BitrateKbps, EncoderType Codec);
public sealed record CreateProfileRequest(string Name, IReadOnlyList<RenditionDto> Renditions);

[Authorize]
public class ProfileEndpoints
{
    [WolverinePost("/api/profiles")]
    public static async Task<IResult> Create(CreateProfileRequest request, IMessageBus bus)
    {
        var command = new CreateProfileCommand(
            request.Name,
            request.Renditions
                .Select(r => new RenditionInput(r.Width, r.Height, r.BitrateKbps, r.Codec))
                .ToList());

        var result = await bus.InvokeAsync<CreateProfileResult>(command);
        return Results.Created($"/api/profiles/{result.ProfileId}", result);
    }
}