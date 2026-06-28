namespace Flux.Transcoder.Contracts.Commands;

public sealed record StartChannelCommand(
    Guid ChannelId,
    string SourceUrl,
    Guid ProfileId);