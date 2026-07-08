namespace Flux.Transcoder.Application.Channels.Queries.GetChannelById;

public sealed record GetChannelByIdQuery(Guid ChannelId);

public sealed record ChannelView(
    Guid ChannelId,
    string SourceUrl,
    Guid ProfileId,
    string Status,
    int RetentionMinutes);