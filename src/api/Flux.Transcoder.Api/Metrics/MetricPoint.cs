namespace Flux.Transcoder.Api.Metrics;

public sealed record MetricPoint(
    DateTimeOffset Time, double AvgBitrate, double MaxBitrate,
    long DroppedFrames, double UptimeSeconds);