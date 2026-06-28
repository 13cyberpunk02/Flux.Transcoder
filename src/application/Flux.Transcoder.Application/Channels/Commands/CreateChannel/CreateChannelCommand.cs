namespace Flux.Transcoder.Application.Channels.Commands.CreateChannel;

public sealed record CreateChannelCommand(string SourceUrl, Guid ProfileId);

public sealed record CreateChannelResult(Guid ChannelId);