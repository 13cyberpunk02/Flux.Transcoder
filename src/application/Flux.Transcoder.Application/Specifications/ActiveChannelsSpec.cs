using Ardalis.Specification;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Application.Specifications;

public sealed class ActiveChannelsSpec : Specification<Channel>
{
    private static readonly ChannelStatus[] Active =
    {
        ChannelStatus.Starting,
        ChannelStatus.Running,
        ChannelStatus.Degraded
    };

    public ActiveChannelsSpec()
    {
        Query.Where(c => Active.Contains(c.Status));
    }
}