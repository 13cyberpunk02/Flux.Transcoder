namespace Flux.Transcoder.Contracts.Commands;

public sealed record StopChannelCommand(
    Guid ChannelId);