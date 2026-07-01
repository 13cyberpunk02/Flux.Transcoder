using Flux.Transcoder.Contracts.Events;
using Flux.Transcoder.Ffmpeg.Runtime;
using Wolverine.Runtime;

namespace Flux.Transcoder.Worker.Services;

public sealed class HealthReportingService(
    ProcessSupervisor supervisor,
    ProgressParser progress,
    SegmentHealthCheck health,
    BitrateMeter bitrate,
    IWolverineRuntime runtime,
    ILogger<HealthReportingService> logger)
    : BackgroundService
{
    // измеритель битрейта
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var proc in supervisor.All())
            {
                var snapshot = progress.Latest(proc.ChannelId);
                var state = health.Check(proc.ChannelId);
                var outputKbps = bitrate.MeasureKbps(proc.ChannelId);

                var report = new ChannelHealthReport(
                    ChannelId: proc.ChannelId,
                    Status: state.ToString(),
                    InputBitrateKbps: outputKbps,
                    OutputBitrateKbps: outputKbps,
                    DroppedFrames: (int)(snapshot?.DroppedFrames ?? 0),
                    UptimeSeconds: (DateTimeOffset.UtcNow - proc.StartedAt).TotalSeconds,
                    ReportedAt: DateTimeOffset.UtcNow);

                try
                {
                    var bus = new MessageBus(runtime);
                    await bus.PublishAsync(report);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка публикации health {ChannelId}", proc.ChannelId);
                }
            }
        }
    }
}