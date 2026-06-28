using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Contracts.Commands;

namespace Flux.Transcoder.Worker.Handlers;

public sealed class StopChannelCommandHandler
{
    public static async Task HandleAsync(
        StopChannelCommand command,
        IStreamOrchestrator orchestrator,
        ILogger<StopChannelCommandHandler>? logger,
        CancellationToken cancellationToken)
    {
        logger?.LogInformation("Останавливаю процесс ffmpeg для канала с Id {ChannelId}", command.ChannelId);
        await orchestrator.StopAsync(command.ChannelId, cancellationToken);
    }
}