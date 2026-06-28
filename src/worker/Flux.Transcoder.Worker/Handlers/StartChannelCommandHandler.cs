using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Contracts.Commands;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Worker.Handlers;

public sealed class StartChannelCommandHandler
{
    public static async Task HandleAsync(
        StartChannelCommand command,
        IReadRepository<Channel> channels,
        IStreamOrchestrator orchestrator,
        ILogger<StartChannelCommandHandler>? logger,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
            new ChannelByIdSpec(command.ChannelId), cancellationToken);

        if (channel is null)
        {
            logger?.LogWarning("Канал с Id {ChannelId} не найдено на стороне службы транскодирования", command.ChannelId);
            return;
        }

        logger?.LogInformation("Запуск процесса ffmpeg для канала с Id {ChannelId}", channel.Id);
        await orchestrator.StartAsync(channel, cancellationToken);
    }
}