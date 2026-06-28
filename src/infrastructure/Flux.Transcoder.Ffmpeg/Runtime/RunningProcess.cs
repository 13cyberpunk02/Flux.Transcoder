using System.Diagnostics;

namespace Flux.Transcoder.Ffmpeg.Runtime;


public sealed record RunningProcess(
    Guid ChannelId,
    int ProcessId,
    DateTimeOffset StartedAt,
    CancellationTokenSource Cts,
    Process Process);