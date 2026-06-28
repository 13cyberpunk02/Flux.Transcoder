using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Worker.Handlers;

public sealed class RestartChannelCommandHandler
{
    public static async Task HandleAsync(
        RestartChannelCommand command,
        IReadRepository<Channel> channels,
        IStreamOrchestrator orchestrator,
        ILogger<RestartChannelCommandHandler>? logger,
        CancellationToken cancellationToken)
    {
        logger?.LogInformation("Перезапускаю процесс ffmpeg для канала с Id {ChannelId}", command.ChannelId);

        await orchestrator.StopAsync(command.ChannelId, cancellationToken);

        var channel = await channels.FirstOrDefaultAsync(
            new ChannelByIdSpec(command.ChannelId), cancellationToken);

        if (channel is not null)
            await orchestrator.StartAsync(channel, cancellationToken);
    }
}