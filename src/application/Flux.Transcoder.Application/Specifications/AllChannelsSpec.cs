using Ardalis.Specification;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Application.Specifications;

public sealed class AllChannelsSpec : Specification<Channel>
{
    public AllChannelsSpec()
    {
        Query.Include(c => c.Profile);
    }
}