using Ardalis.Specification;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Specifications;

public sealed class AllProfilesSpec : Specification<EncodingProfile>
{
    public AllProfilesSpec()
    {
        Query.Include(p => p.Renditions);
    }
}