using Flux.Transcoder.Application.Channels.Commands.CreateChannel;
using Flux.Transcoder.Application.Channels.Commands.RestartChannel;
using Flux.Transcoder.Application.Channels.Commands.StartChannel;
using Flux.Transcoder.Application.Channels.Commands.StopChannel;
using Flux.Transcoder.Application.Channels.Commands.UpdateRetention;
using Flux.Transcoder.Application.Channels.Queries.GetAllChannels;
using Flux.Transcoder.Application.Channels.Queries.GetChannelById;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

public sealed record CreateChannelRequest(string SourceUrl, Guid ProfileId);
public sealed record UpdateRetentionRequest(int RetentionMinutes);

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
    
    [WolverineGet("/api/channels")]
    public static async Task<IReadOnlyList<ChannelView>> GetAll(IMessageBus bus)
        => await bus.InvokeAsync<IReadOnlyList<ChannelView>>(new GetAllChannelsQuery());

    [WolverineGet("/api/channels/{id:guid}")]
    public static async Task<IResult> GetById(Guid id, IMessageBus bus)
    {
        var view = await bus.InvokeAsync<ChannelView?>(new GetChannelByIdQuery(id));
        return view is null ? Results.NotFound() : Results.Ok(view);
    }


    [WolverinePost("/api/channels/{id:guid}/start")]
    public static async Task<IResult> Start(Guid id, IMessageBus bus)
    {
        await bus.InvokeAsync(new StartChannelRequest(id));
        return Results.Accepted($"/api/channels/{id}");
    }

    [WolverinePost("/api/channels/{id:guid}/stop")]
    public static async Task<IResult> Stop(Guid id, IMessageBus bus)
    {
        await bus.InvokeAsync(new StopChannelRequest(id));
        return Results.Accepted($"/api/channels/{id}");
    }

    [WolverinePost("/api/channels/{id:guid}/restart")]
    public static async Task<IResult> Restart(Guid id, IMessageBus bus)
    {
        await bus.InvokeAsync(new RestartChannelRequest(id));
        return Results.Accepted($"/api/channels/{id}");
    }
    
    [WolverinePut("/api/channels/{id:guid}/retention")]
    public static async Task<IResult> UpdateRetention(
        Guid id, UpdateRetentionRequest request, IMessageBus bus)
    {
        await bus.InvokeAsync(new UpdateRetentionCommand(id, request.RetentionMinutes));
        return Results.NoContent();
    }
}