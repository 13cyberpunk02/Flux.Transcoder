using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Worker.Services;

namespace Flux.Transcoder.Worker.Handlers;

/// <summary>Команда — только сигнал: что делать с каналом, реконсилер читает из БД.</summary>
public sealed class StopChannelCommandHandler
{
    public static void Handle(
        StopChannelCommand command,
        ChannelReconciler reconciler,
        ILogger<StopChannelCommandHandler> logger)
    {
        logger.LogInformation("StopChannelCommand для канала с Id {ChannelId}", command.ChannelId);
        reconciler.Trigger(command.ChannelId);
    }
}
