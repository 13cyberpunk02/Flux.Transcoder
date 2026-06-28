using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Domain.Channels.Events;

public sealed record ChannelRunning(Guid ChannelId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}