using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Channels.Commands.StartChannel;

public class StartChannelHandler
{
    public static async Task HandleAsync(
        StartChannelRequest request,
        IRepository<Channel> channels,
        ITransactionalPublisher publisher,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
                          new ChannelByIdSpec(request.ChannelId), cancellationToken)
                      ?? throw new NotFoundException($"Канал с Id {request.ChannelId} не найден.");

        channel.Start();                                  // Idle/Stopped/Failed -> Starting

        // статус и КОНТРАКТНАЯ команда — одной транзакцией (outbox)
        await publisher.SaveAndPublishAsync(new Contracts.Commands.StartChannelCommand(
            channel.Id,
            channel.Source.Value,
            channel.Profile.Id), cancellationToken);
    }
}