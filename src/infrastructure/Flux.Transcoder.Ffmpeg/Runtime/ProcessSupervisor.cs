using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class ProcessSupervisor(ILogger<ProcessSupervisor> logger)
{
    private static readonly TimeSpan GracefulStopTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ShutdownTimeout = GracefulStopTimeout + TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<Guid, RunningProcess> _processes = new();
    private volatile bool _closing;

    /// <summary>Воркер выключается: новые процессы не запускаем.</summary>
    public bool IsClosing => _closing;

    public RunningProcess Register(Guid channelId, int revision, int pid, Process process)
    {
        var running = new RunningProcess(channelId, revision, pid, DateTimeOffset.UtcNow, process);
        _processes[channelId] = running;
        return running;
    }

    /// <summary>
    /// Убирает запись, только если она всё ещё указывает на ЭТОТ процесс.
    /// Иначе запоздавший Exited старого процесса удалил бы запись нового (после рестарта).
    /// Сам Process освобождает обработчик Exited.
    /// </summary>
    public void Unregister(RunningProcess running) =>
        _processes.TryRemove(new KeyValuePair<Guid, RunningProcess>(running.ChannelId, running));

    /// <summary>
    /// Начинает остановку и сразу возвращает управление; задача завершится после остановки.
    /// Ждать её нельзя: зависший на чтении источника ffmpeg не реагирует на 'q',
    /// и каждая остановка задерживала бы остальные каналы на 5 сек.
    /// </summary>
    public Task Stop(RunningProcess running)
    {
        if (!running.TryRequestStop())
            return running.Exited;

        logger.LogInformation(
            "Stopping channel {ChannelId} (pid {Pid})", running.ChannelId, running.ProcessId);
        return Task.Run(() => TerminateAsync(running));
    }

    /// <summary>
    /// Штатное выключение: останавливает всё через 'q', чтобы сегменты дописались.
    /// Если воркер убит, сюда мы не попадём — тогда работает ChildProcessGuard.
    /// </summary>
    public async Task ShutdownAsync()
    {
        _closing = true;
        var timeout = Task.Delay(ShutdownTimeout);

        // циклом: Launch, начатый до _closing, мог успеть зарегистрировать ещё один процесс
        while (All() is { Count: > 0 } running && !timeout.IsCompleted)
        {
            foreach (var process in running)
                _ = Stop(process);

            await Task.WhenAny(Task.WhenAll(running.Select(p => p.Exited)), timeout);
        }
    }

    public RunningProcess? Get(Guid channelId) =>
        _processes.GetValueOrDefault(channelId);

    public IReadOnlyCollection<RunningProcess> All() => _processes.Values.ToList();

    private async Task TerminateAsync(RunningProcess running)
    {
        try
        {
            if (running.HasExited) return;

            // 'q' в stdin — штатное завершение ffmpeg: дописывает сегмент и плейлист
            try
            {
                await running.Process.StandardInput.WriteAsync('q');
                await running.Process.StandardInput.FlushAsync();
            }
            catch (IOException)
            {
                // stdin уже закрыт — дождёмся выхода или убьём по таймауту
            }

            if (await Task.WhenAny(running.Exited, Task.Delay(GracefulStopTimeout)) == running.Exited)
                return;

            logger.LogWarning(
                "ffmpeg {ChannelId} не завершился по 'q' за {Timeout:g} — убиваю",
                running.ChannelId, GracefulStopTimeout);
            running.Process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // процесс уже завершился или освобождён обработчиком Exited (ObjectDisposedException)
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error terminating ffmpeg {ChannelId}", running.ChannelId);
        }
    }
}
