using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class ProcessSupervisor(ILogger<ProcessSupervisor> logger)
{
    private readonly ConcurrentDictionary<Guid, RunningProcess> _processes = new();

    public bool IsRunning(Guid channelId) => _processes.ContainsKey(channelId);

    public RunningProcess Register(Guid channelId, int pid, CancellationTokenSource cts, Process process)
    {
        var running = new RunningProcess(channelId, pid, DateTimeOffset.UtcNow, cts, process);
        _processes[channelId] = running;
        return running;
    }

    /// <summary>
    /// Убирает запись, только если она всё ещё указывает на ЭТОТ процесс.
    /// Иначе запоздавший Exited старого процесса удалил бы запись нового (после рестарта).
    /// Сам Process освобождает обработчик Exited.
    /// </summary>
    public void Unregister(RunningProcess running)
    {
        if (_processes.TryRemove(new KeyValuePair<Guid, RunningProcess>(running.ChannelId, running)))
            running.Cts.Dispose();
    }

    public void Stop(Guid channelId)
    {
        if (_processes.TryGetValue(channelId, out var proc))
        {
            proc.IntentionalStop = true;
            logger.LogInformation(
                "Stopping channel {ChannelId} (pid {Pid})", channelId, proc.ProcessId);
            try
            {
                proc.Cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // запись уже снята параллельно — процесс завершён
            }
        }
    }

    public RunningProcess? Get(Guid channelId) =>
        _processes.GetValueOrDefault(channelId);

    public IReadOnlyCollection<RunningProcess> All() => _processes.Values.ToList();
}
