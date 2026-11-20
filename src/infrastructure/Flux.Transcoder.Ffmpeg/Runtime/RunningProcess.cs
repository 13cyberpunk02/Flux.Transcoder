using System.Diagnostics;

namespace Flux.Transcoder.Ffmpeg.Runtime;


public sealed class RunningProcess(
    Guid channelId,
    int processId,
    DateTimeOffset startedAt,
    CancellationTokenSource cts,
    Process process)
{
    private volatile bool _intentionalStop;

    public Guid ChannelId { get; } = channelId;
    public int ProcessId { get; } = processId;
    public DateTimeOffset StartedAt { get; } = startedAt;
    public CancellationTokenSource Cts { get; } = cts;
    public Process Process { get; } = process;

    /// <summary>true, если остановка инициирована через StopAsync (намеренно).</summary>
    public bool IntentionalStop
    {
        get => _intentionalStop;
        set => _intentionalStop = value;
    }

    /// <summary>
    /// Безопасная проверка завершения: Process может быть уже освобождён
    /// обработчиком Exited — тогда считаем, что процесс завершился.
    /// </summary>
    public bool HasExited
    {
        get
        {
            try { return Process.HasExited; }
            catch (InvalidOperationException) { return true; }   // в т.ч. ObjectDisposedException
        }
    }
}
