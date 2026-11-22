using Flux.Transcoder.Ffmpeg.Runtime;

namespace Flux.Transcoder.Worker.Services;

public sealed class ProcessReaperService(
    ProcessSupervisor supervisor,
    SegmentHealthCheck health,
    RestartCoordinator restartCoordinator,
    ILogger<ProcessReaperService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var proc in supervisor.All())
            {
                try
                {
                    // уже останавливается: запись снимет Exited, он же запустит отложенный старт
                    if (proc.IntentionalStop)
                        continue;

                    // процесс уже умер, но запись висит — подчищаем
                    if (proc.HasExited)
                    {
                        logger.LogWarning(
                            "Процесс уже умер но запись висит для канала с Id {ChannelId} (pid {Pid})",
                            proc.ChannelId, proc.ProcessId);
                        supervisor.Unregister(proc);
                        continue;
                    }

                    var age = DateTimeOffset.UtcNow - proc.StartedAt;
                    if (age < TimeSpan.FromSeconds(20))
                        continue;

                    var state = health.Check(proc.ChannelId);
                    if (state == HealthState.Stalled)
                    {
                        logger.LogWarning(
                            "Канал {ChannelId} завис — свежие сегменты не приходят", proc.ChannelId);
                        _ = supervisor.Stop(proc);   // выхода не ждём: обход остальных каналов не задерживается
                        restartCoordinator.OnStalled(proc.ChannelId);
                    }
                    else if (state == HealthState.Healthy)
                    {
                        restartCoordinator.OnHealthy(proc.ChannelId);
                    }
                }
                catch (Exception ex)
                {
                    // исключение в BackgroundService остановило бы весь воркер
                    logger.LogError(ex, "Ошибка проверки канала {ChannelId}", proc.ChannelId);
                }
            }
        }
    }
}