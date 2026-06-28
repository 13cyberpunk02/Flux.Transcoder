namespace Flux.Transcoder.Contracts.Events;

public sealed record ChannelStateChanged(
    Guid ChannelId,
    string PreviousStatus,
    string CurrentStatus,
    DateTimeOffset OccurredOn,
    string? Reason = null);