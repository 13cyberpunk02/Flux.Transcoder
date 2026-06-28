using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Domain.Channels.Events;

public sealed record ChannelFailed(Guid ChannelId, string Reason) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}