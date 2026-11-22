using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Channels.Commands.StopChannel;

public class StopChannelHandler
{
    public static async Task HandleAsync(
        StopChannelRequest request,
        IRepository<Channel> channels,
        ITransactionalPublisher publisher,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
                          new ChannelByIdSpec(request.ChannelId), cancellationToken)
                      ?? throw new NotFoundException($"Канал с Id {request.ChannelId} не найден.");

        channel.Stop();
        await publisher.SaveAndPublishAsync(new StopChannelCommand(channel.Id), cancellationToken);
    }
}