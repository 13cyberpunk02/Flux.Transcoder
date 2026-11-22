using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Worker.Services;

namespace Flux.Transcoder.Worker.Handlers;

/// <summary>Команда — только сигнал: что делать с каналом, реконсилер читает из БД.</summary>
public sealed class RestartChannelCommandHandler
{
    public static void Handle(
        RestartChannelCommand command,
        ChannelReconciler reconciler,
        ILogger<RestartChannelCommandHandler> logger)
    {
        logger.LogInformation("RestartChannelCommand для канала с Id {ChannelId}", command.ChannelId);
        reconciler.Trigger(command.ChannelId);
    }
}
