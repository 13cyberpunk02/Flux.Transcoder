using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;
using Wolverine;
using BusContracts = Flux.Transcoder.Contracts.Commands;

namespace Flux.Transcoder.Application.Channels.Commands.StartChannel;

public static class StartChannelHandler
{
    public static async Task HandleAsync(
        StartChannelCommand command,
        IRepository<Channel> channels,
        IUnitOfWork unitOfWork,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
                          new ChannelByIdSpec(command.ChannelId), cancellationToken)
                      ?? throw new DomainException($"Канал {command.ChannelId} не найден.");

        channel.Start();                  // доменный переход Idle -> Starting (+ событие)
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // API только ПРОСИТ воркера запустить поток — сам FFmpeg здесь не трогаем
        await bus.PublishAsync(new BusContracts.StartChannelCommand(
            channel.Id,
            channel.Source.Value,
            channel.Profile.Id));
    }
}