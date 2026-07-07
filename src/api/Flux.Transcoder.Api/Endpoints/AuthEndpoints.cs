using Flux.Transcoder.Application.Users.Commands.Login;
using Microsoft.AspNetCore.Authorization;
using Wolverine;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

public static class AuthEndpoints
{
    [WolverinePost("/api/auth/login")]
    [AllowAnonymous]
    public static async Task<IResult> Login(LoginCommand command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<LoginResult>(command);
        return Results.Ok(result);
    }
}