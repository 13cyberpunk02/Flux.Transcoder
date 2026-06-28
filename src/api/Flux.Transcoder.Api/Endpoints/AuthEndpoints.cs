using Flux.Transcoder.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token);

public static class AuthEndpoints
{
    [WolverinePost("/api/auth/login")]
    [AllowAnonymous]
    public static IResult Login(LoginRequest request, ITokenGenerator tokens)
    {
        // ДЕМО-проверка. В реальности — пользовательское хранилище/хеши.
        if (request.Username != "admin" || request.Password != "admin")
            return Results.Unauthorized();

        // токен здесь не привязан к каналу — генерим «сессионный»;
        // если ITokenGenerator завязан на channelId, заведи перегрузку
        var token = tokens.Generate(Guid.Empty, TimeSpan.FromHours(8));
        return Results.Ok(new LoginResponse(token));
    }
}