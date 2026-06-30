using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Channels.Queries.GetChannelById;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Application.Channels.Queries.GetAllChannels;

public class GetAllChannelsHandler
{
    public static async Task<IReadOnlyList<ChannelView>> HandleAsync(
        GetAllChannelsQuery query,
        IReadRepository<Channel> channels,
        CancellationToken cancellationToken)
    {
        var list = await channels.ListAsync(new AllChannelsSpec(), cancellationToken);

        return list.Select(c => new ChannelView(
            c.Id, c.Source.Value, c.Profile.Id, c.Status.ToString())).ToList();
    }
}