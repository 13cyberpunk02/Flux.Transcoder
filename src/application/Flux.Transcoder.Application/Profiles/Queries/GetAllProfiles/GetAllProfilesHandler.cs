using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Profiles.Queries.GetAllProfiles;


public class GetAllProfilesHandler
{
    public static async Task<IReadOnlyList<ProfileView>> HandleAsync(
        GetAllProfilesQuery query,
        IReadRepository<EncodingProfile> profiles,
        CancellationToken cancellationToken)
    {
        var list = await profiles.ListAsync(new AllProfilesSpec(), cancellationToken);

        return list.Select(p => new ProfileView(
            p.Id,
            p.Name,
            p.Renditions.Select(r => new RenditionView(
                r.Width, r.Height, r.BitrateKbps, r.Codec.ToString())).ToList()
        )).ToList();
    }
}