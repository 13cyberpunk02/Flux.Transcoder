using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Common;
using Flux.Transcoder.Domain.Users;

namespace Flux.Transcoder.Application.Users.Commands.Login;

public class LoginHandler
{
    public static async Task<LoginResult> HandleAsync(
        LoginCommand command,
        IReadRepository<User> users,
        IPasswordHasher hasher,
        IAuthTokenService tokens,
        CancellationToken ct)
    {
        var user = await users.FirstOrDefaultAsync(new UserByUsernameSpec(command.Username), ct)
                   ?? throw new UnauthorizedException("Неверный логин или пароль.");

        if (!hasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedException("Неверный логин или пароль.");

        var token = tokens.GenerateForUser(user.Id, user.Username, TimeSpan.FromHours(8));
        return new LoginResult(token);
    }
}