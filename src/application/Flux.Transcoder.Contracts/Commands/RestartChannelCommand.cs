namespace Flux.Transcoder.Contracts.Commands;

public sealed record RestartChannelCommand(
    Guid ChannelId);