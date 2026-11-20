using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;
using Wolverine;

namespace Flux.Transcoder.Application.Channels.Commands.RestartChannel;

public class RestartChannelHandler
{
    public static async Task HandleAsync(
        RestartChannelRequest request,
        IRepository<Channel> channels,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
                          new ChannelByIdSpec(request.ChannelId), cancellationToken)
                      ?? throw new NotFoundException($"Канал с Id {request.ChannelId} не найден.");

        // * -> Starting: иначе после рестарта из Failed канал навсегда остался бы Failed
        channel.Restart();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new RestartChannelCommand(channel.Id));
    }
}
