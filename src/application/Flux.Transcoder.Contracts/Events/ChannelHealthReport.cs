namespace Flux.Transcoder.Contracts.Events;

public sealed record ChannelHealthReport(
    Guid ChannelId,
    string Status,
    double InputBitrateKbps,
    double OutputBitrateKbps,
    int DroppedFrames,
    double UptimeSeconds,
    DateTimeOffset ReportedAt,
    int RunRevision = 0);