using Flux.Transcoder.Ffmpeg.Configuration;
using Flux.Transcoder.Ffmpeg.Runtime;
using Microsoft.Extensions.Options;

namespace Flux.Transcoder.Worker.Services;

public sealed class ProcessReaperService(
    ProcessSupervisor supervisor,
    SegmentHealthCheck health,
    RestartCoordinator restartCoordinator,
    IOptions<FfmpegOptions> options,
    ILogger<ProcessReaperService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _startupTimeout = TimeSpan.FromSeconds(options.Value.StartupTimeoutSeconds);

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
                        restartCoordinator.OnFailure(proc.ChannelId, proc.Revision);
                        supervisor.Unregister(proc);
                        continue;
                    }

                    var age = DateTimeOffset.UtcNow - proc.StartedAt;
                    if (age < TimeSpan.FromSeconds(20))
                        continue;

                    var state = health.Check(proc.ChannelId, proc.StartedAt);
                    var noFirstSegment = state == HealthState.Unknown && age >= _startupTimeout;

                    if (state == HealthState.Stalled || noFirstSegment)
                    {
                        if (noFirstSegment)
                            logger.LogWarning(
                                "Канал {ChannelId}: первый сегмент не появился за {Timeout:g}", proc.ChannelId, _startupTimeout);
                        else
                            logger.LogWarning(
                                "Канал {ChannelId} завис — свежие сегменты не приходят", proc.ChannelId);

                        // сначала пауза, потом Stop: иначе сверка успела бы перезапустить без backoff
                        restartCoordinator.OnFailure(proc.ChannelId, proc.Revision);
                        _ = supervisor.Stop(proc);   // выхода не ждём: обход остальных каналов не задерживается
                    }
                    else if (state == HealthState.Healthy)
                    {
                        restartCoordinator.OnHealthy(proc.ChannelId, proc.Revision);
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