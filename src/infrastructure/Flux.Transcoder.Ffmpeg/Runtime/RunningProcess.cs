using System.Diagnostics;
using Flux.Transcoder.Domain.Channels;

namespace Flux.Transcoder.Ffmpeg.Runtime;


public sealed class RunningProcess(
    Guid channelId,
    int revision,
    int processId,
    DateTimeOffset startedAt,
    Process process)
{
    private readonly TaskCompletionSource _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _stopRequested;

    public Guid ChannelId { get; } = channelId;

    /// <summary>Ревизия запуска канала (Channel.RunRevision), для которой запущен процесс.</summary>
    public int Revision { get; } = revision;

    public int ProcessId { get; } = processId;
    public DateTimeOffset StartedAt { get; } = startedAt;
    public Process Process { get; } = process;

    /// <summary>
    /// Канал, который надо запустить, когда этот процесс выйдет (Start пришёл во время остановки).
    /// Читается и меняется только под блокировкой канала в оркестраторе.
    /// </summary>
    internal Channel? StartAfterExit { get; set; }

    /// <summary>Завершается, когда обработчик Exited закончил работу с процессом.</summary>
    internal Task Exited => _exited.Task;

    internal void MarkExited() => _exited.TrySetResult();

    /// <summary>true, если процесс останавливаем мы сами (пользователь, reaper или выключение воркера).</summary>
    public bool IntentionalStop => Volatile.Read(ref _stopRequested) == 1;

    /// <summary>true только у первого вызвавшего — он и останавливает процесс.</summary>
    internal bool TryRequestStop() => Interlocked.Exchange(ref _stopRequested, 1) == 0;

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
