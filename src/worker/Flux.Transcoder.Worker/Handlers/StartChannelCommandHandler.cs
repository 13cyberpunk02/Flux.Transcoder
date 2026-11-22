using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Worker.Services;

namespace Flux.Transcoder.Worker.Handlers;

/// <summary>Команда — только сигнал: что делать с каналом, реконсилер читает из БД.</summary>
public sealed class StartChannelCommandHandler
{
    public static void Handle(
        StartChannelCommand command,
        ChannelReconciler reconciler,
        ILogger<StartChannelCommandHandler> logger)
    {
        logger.LogInformation("StartChannelCommand для канала с Id {ChannelId}", command.ChannelId);
        reconciler.Trigger(command.ChannelId);
    }
}
