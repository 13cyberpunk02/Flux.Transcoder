using Flux.Transcoder.Contracts.Events;
using Microsoft.AspNetCore.SignalR;

namespace Flux.Transcoder.Api.Hubs;

/// <summary>Смена статуса, сделанная воркером (Failed), — иначе в UI канал остался бы «Запуск».</summary>
public class ChannelStateChangedHandler
{
    public static Task Handle(
        ChannelStateChanged change,
        IHubContext<ChannelsHub> hub,
        CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync("channelStatusChanged",
            new { channelId = change.ChannelId, status = change.CurrentStatus },
            cancellationToken);
}
