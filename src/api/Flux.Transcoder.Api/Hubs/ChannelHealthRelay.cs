using Flux.Transcoder.Contracts.Events;
using Microsoft.AspNetCore.SignalR;

namespace Flux.Transcoder.Api.Hubs;

public class ChannelHealthRelay
{
    public static async Task HandleAsync(
        ChannelHealthReport report,
        IHubContext<ChannelsHub> hub,
        CancellationToken cancellationToken)
    {
        await hub.Clients.All.SendAsync("channelHealth", report, cancellationToken);
    }
}