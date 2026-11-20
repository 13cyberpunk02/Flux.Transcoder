using Flux.Transcoder.Api.Metrics;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Contracts.Events;
using Flux.Transcoder.Domain.Channels;
using Microsoft.AspNetCore.SignalR;

namespace Flux.Transcoder.Api.Hubs;

public class ChannelHealthHandler
{
    public static async Task Handle(
        ChannelHealthReport report,
        IHubContext<ChannelsHub> hub,
        IRepository<Channel> channels,
        IUnitOfWork unitOfWork,
        MetricsWriter metrics, 
        CancellationToken cancellationToken)
    {
        await hub.Clients.All.SendAsync("channelHealth", report, cancellationToken);
        
        await metrics.WriteAsync(
            report.ChannelId, report.OutputBitrateKbps, report.DroppedFrames,
            report.UptimeSeconds, report.Status, cancellationToken);
        
        var channel = await channels.FirstOrDefaultAsync(
            new ChannelByIdSpec(report.ChannelId), cancellationToken);

        if (channel is null) return;

        if (report.Status == "Healthy" && channel.Status is ChannelStatus.Starting or ChannelStatus.Degraded)
        {
            channel.MarkRunning();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await hub.Clients.All.SendAsync("channelStatusChanged",
                new { channelId = channel.Id, status = channel.Status.ToString() },
                cancellationToken);
        }
    }
}