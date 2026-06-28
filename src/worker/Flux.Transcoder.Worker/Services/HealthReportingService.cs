using Flux.Transcoder.Contracts.Events;
using Flux.Transcoder.Ffmpeg.Runtime;
using Wolverine;

namespace Flux.Transcoder.Worker.Services;

public sealed class HealthReportingService : BackgroundService
{
    private readonly ProcessSupervisor _supervisor;       // singleton — можно в конструктор
    private readonly ProgressParser _progress;            // singleton
    private readonly SegmentHealthCheck _health;          // singleton
    private readonly IServiceProvider _services;          // фабрика scope'ов
    private readonly ILogger<HealthReportingService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public HealthReportingService(
        ProcessSupervisor supervisor,
        ProgressParser progress,
        SegmentHealthCheck health,
        IServiceProvider services,
        ILogger<HealthReportingService> logger)
    {
        _supervisor = supervisor;
        _progress = progress;
        _health = health;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var proc in _supervisor.All())
            {
                var snapshot = _progress.Latest(proc.ChannelId);
                var state = _health.Check(proc.ChannelId);

                var report = new ChannelHealthReport(
                    ChannelId: proc.ChannelId,
                    Status: state.ToString(),
                    InputBitrateKbps: snapshot?.BitrateKbps ?? 0,
                    OutputBitrateKbps: snapshot?.BitrateKbps ?? 0,
                    DroppedFrames: (int)(snapshot?.DroppedFrames ?? 0),
                    UptimeSeconds: (DateTimeOffset.UtcNow - proc.StartedAt).TotalSeconds,
                    ReportedAt: DateTimeOffset.UtcNow);

                try
                {
                    // открываем scope на каждую публикацию — берём scoped IMessageBus отсюда
                    await using var scope = _services.CreateAsyncScope();
                    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                    await bus.PublishAsync(report);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при публикации отчёта о здоровья канала с Id {ChannelId}", proc.ChannelId);
                }
            }
        }
    }
}