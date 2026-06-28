using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Domain.Common;
using Flux.Transcoder.Domain.Encoding;
using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

public sealed record RenditionDto(int Width, int Height, int BitrateKbps, EncoderType Codec);
public sealed record CreateProfileRequest(string Name, IReadOnlyList<RenditionDto> Renditions);
public sealed record ProfileCreatedResponse(Guid ProfileId);

[Authorize]
public static class ProfileEndpoints
{
    [WolverinePost("/api/profiles")]
    public static async Task<IResult> Create(
        CreateProfileRequest request,
        IRepository<EncodingProfile> profiles,
        IUnitOfWork unitOfWork)
    {
        var renditions = request.Renditions
            .Select(r => new Rendition(r.Width, r.Height, r.BitrateKbps, r.Codec));

        var profile = new EncodingProfile(request.Name, renditions); // кинет DomainException если пусто
        await profiles.AddAsync(profile);
        await unitOfWork.SaveChangesAsync();

        return Results.Created($"/api/profiles/{profile.Id}", new ProfileCreatedResponse(profile.Id));
    }
}