using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;
using Wolverine;

namespace Flux.Transcoder.Application.Channels.Commands.StartChannel;

public class StartChannelHandler
{
    public static async Task HandleAsync(
        StartChannelRequest request,
        IRepository<Channel> channels,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
                          new ChannelByIdSpec(request.ChannelId), cancellationToken)
                      ?? throw new NotFoundException($"Канал с Id {request.ChannelId} не найден.");

        channel.Start();                                  // Idle -> Starting
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // публикуем КОНТРАКТНУЮ команду в Rabbit -> её поймает воркер
        await bus.PublishAsync(new Contracts.Commands.StartChannelCommand(
            channel.Id,
            channel.Source.Value,
            channel.Profile.Id));
    }
}