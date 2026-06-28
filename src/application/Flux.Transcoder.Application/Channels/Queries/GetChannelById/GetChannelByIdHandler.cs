using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Application.Channels.Queries.GetChannelById;

public static class GetChannelByIdHandler
{
    public static async Task<ChannelView?> HandleAsync(
        GetChannelByIdQuery query,
        IReadRepository<Channel> channels,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
            new ChannelByIdSpec(query.ChannelId), cancellationToken);

        return channel is null
            ? null
            : new ChannelView(
                channel.Id,
                channel.Source.Value,
                channel.Profile.Id,
                channel.Status.ToString());
    }
}