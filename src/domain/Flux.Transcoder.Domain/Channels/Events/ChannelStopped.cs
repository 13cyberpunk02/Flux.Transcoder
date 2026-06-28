using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Domain.Channels.Events;

public sealed record ChannelStopped(Guid ChannelId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}