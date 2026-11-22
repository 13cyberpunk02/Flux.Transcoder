using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class ProcessSupervisor(ILogger<ProcessSupervisor> logger)
{
    private static readonly TimeSpan GracefulStopTimeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<Guid, RunningProcess> _processes = new();

    public RunningProcess Register(Guid channelId, int pid, Process process)
    {
        var running = new RunningProcess(channelId, pid, DateTimeOffset.UtcNow, process);
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
    /// Ждать её в обработчике команд нельзя: очередь одна на все каналы, а зависший на чтении
    /// источника ffmpeg не реагирует на 'q' — каждый Stop задерживал бы всех на 5 сек.
    /// </summary>
    public Task Stop(RunningProcess running)
    {
        if (!running.TryRequestStop())
            return running.Exited;

        logger.LogInformation(
            "Stopping channel {ChannelId} (pid {Pid})", running.ChannelId, running.ProcessId);
        return Task.Run(() => TerminateAsync(running));
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
