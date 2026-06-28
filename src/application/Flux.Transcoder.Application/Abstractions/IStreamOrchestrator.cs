using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Application.Abstractions;

public interface IStreamOrchestrator
{
    Task StartAsync(Channel channel, CancellationToken cancellationToken = default);
    Task StopAsync(Guid channelId, CancellationToken cancellationToken = default);
}