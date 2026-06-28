namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed record ProgressSnapshot(
    Guid ChannelId,
    long Frame,
    double Fps,
    double BitrateKbps,
    TimeSpan OutTime,
    long DroppedFrames,
    DateTimeOffset CapturedAt);