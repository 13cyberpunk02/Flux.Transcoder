using Flux.Transcoder.Ffmpeg.Runtime;

namespace Flux.Transcoder.Worker.Services;

public sealed class ProcessReaperService(
    ProcessSupervisor supervisor,
    SegmentHealthCheck health,
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

                // источник завис: сегменты перестали появляться, но процесс жив —
                // это кандидат на restart policy (см. Фазу 5.7)
                if (health.Check(proc.ChannelId) == HealthState.Stalled)
                {
                    logger.LogWarning(
                        "Канал с Id {ChannelId} завис — свежие видео сегменты не приходят", proc.ChannelId);
                    // здесь точка интеграции с RestartPolicy:
                    // _supervisor.Stop(proc.ChannelId) + запланировать рестарт с backoff
                }
            }
        }
    }
}