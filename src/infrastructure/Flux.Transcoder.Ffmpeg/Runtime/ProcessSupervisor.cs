using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class ProcessSupervisor(ILogger<ProcessSupervisor> logger)
{
    private readonly ConcurrentDictionary<Guid, RunningProcess> _processes = new();

    public bool IsRunning(Guid channelId) => _processes.ContainsKey(channelId);

    public void Register(Guid channelId, int pid, CancellationTokenSource cts, Process process)
    {
        _processes[channelId] = new RunningProcess(
            channelId, pid, DateTimeOffset.UtcNow, cts, process);
    }

    public void Unregister(Guid channelId)
    {
        if (_processes.TryRemove(channelId, out var proc))
        {
            proc.Cts.Dispose();
            proc.Process.Dispose();
        }
    }

    public void Stop(Guid channelId)
    {
        if (_processes.TryGetValue(channelId, out var proc))
        {
            proc.IntentionalStop = true;
            logger.LogInformation(
                "Stopping channel {ChannelId} (pid {Pid})", channelId, proc.ProcessId);
            proc.Cts.Cancel();
        }
    }

    public RunningProcess? Get(Guid channelId) =>
        _processes.GetValueOrDefault(channelId);
    
    /// <summary>Проверить, был ли стоп намеренным (для обработчика Exited).</summary>
    public bool WasIntentionalStop(Guid channelId) =>
        _processes.TryGetValue(channelId, out var proc) && proc.IntentionalStop;

    public IReadOnlyCollection<RunningProcess> All() => _processes.Values.ToList();
}