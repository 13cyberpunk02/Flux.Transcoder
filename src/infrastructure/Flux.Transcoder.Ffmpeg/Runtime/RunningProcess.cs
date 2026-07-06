using System.Diagnostics;

namespace Flux.Transcoder.Ffmpeg.Runtime;


public sealed class RunningProcess(
    Guid channelId,
    int processId,
    DateTimeOffset startedAt,
    CancellationTokenSource cts,
    Process process)
{
    public Guid ChannelId { get; } = channelId;
    public int ProcessId { get; } = processId;
    public DateTimeOffset StartedAt { get; } = startedAt;
    public CancellationTokenSource Cts { get; } = cts;
    public Process Process { get; } = process;

    /// <summary>true, если остановка инициирована через StopAsync (намеренно).</summary>
    public bool IntentionalStop { get; set; } = false;
}