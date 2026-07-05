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
                // процесс уже умер, но запись висит — подчищаем
                if (proc.Process.HasExited)
                {
                    logger.LogWarning(
                        "Процесс уже умер но запись висит для канала с Id {ChannelId} (pid {Pid})",
                        proc.ChannelId, proc.ProcessId);
                    supervisor.Unregister(proc.ChannelId);
                    continue;
                }

                if (health.Check(proc.ChannelId) == HealthState.Stalled)
                {
                    logger.LogWarning(
                        "Канал {ChannelId} завис — свежие сегменты не приходят", proc.ChannelId);
                    restartCoordinator.OnStalled(proc.ChannelId);
                }
                else if (health.Check(proc.ChannelId) == HealthState.Healthy)
                {
                    restartCoordinator.OnHealthy(proc.ChannelId);
                }
            }
        }
    }
}