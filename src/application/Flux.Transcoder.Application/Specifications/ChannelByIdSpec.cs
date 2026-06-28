using Ardalis.Specification;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Application.Specifications;

public sealed class ChannelByIdSpec : Specification<Channel>, ISingleResultSpecification<Channel>
{
    public ChannelByIdSpec(Guid channelId)
    {
        Query.Where(c => c.Id == channelId);
    }
}