using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Flux.Transcoder.Api.Hubs;

/// <summary>Статусы и метрики всех каналов — только вошедшим пользователям.</summary>
[Authorize]
public sealed class ChannelsHub : Hub { }
