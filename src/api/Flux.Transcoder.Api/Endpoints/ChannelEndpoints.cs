using Flux.Transcoder.Application.Channels.Commands.CreateChannel;
using Flux.Transcoder.Application.Channels.Queries.GetChannelById;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using Wolverine.Http;
using AppStart = Flux.Transcoder.Application.Channels.Commands.StartChannel;

namespace Flux.Transcoder.Api.Endpoints;

public sealed record CreateChannelRequest(string SourceUrl, Guid ProfileId);

[Authorize]
public static class ChannelEndpoints
{
    [WolverinePost("/api/channels")]
    public static async Task<IResult> Create(
        CreateChannelRequest request,
        IMessageBus bus)
    {
        var result = await bus.InvokeAsync<CreateChannelResult>(
            new CreateChannelCommand(request.SourceUrl, request.ProfileId));

        return Results.Created($"/api/channels/{result.ChannelId}", result);
    }

    [WolverineGet("/api/channels/{id:guid}")]
    public static async Task<IResult> GetById(Guid id, IMessageBus bus)
    {
        var view = await bus.InvokeAsync<ChannelView?>(new GetChannelByIdQuery(id));
        return view is null ? Results.NotFound() : Results.Ok(view);
    }

    [WolverinePost("/api/channels/{id:guid}/start")]
    public static async Task<IResult> Start(Guid id, IMessageBus bus)
    {
        // внутренняя команда Application; хендлер сам опубликует
        // контрактную StartChannelCommand в RabbitMQ
        await bus.InvokeAsync(new AppStart.StartChannelCommand(id));
        return Results.Accepted($"/api/channels/{id}");
    }

    [WolverinePost("/api/channels/{id:guid}/stop")]
    public static async Task<IResult> Stop(Guid id, IMessageBus bus)
    {
        // здесь понадобится StopChannel-хендлер в Application
        // (симметричен StartChannel: channel.Stop() + publish StopChannelCommand)
        await bus.InvokeAsync(new AppStart.StartChannelCommand(id)); // TODO заменить на StopChannelCommand
        return Results.Accepted($"/api/channels/{id}");
    }
}