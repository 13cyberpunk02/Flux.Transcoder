using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Common;
using Flux.Transcoder.Domain.Users;

namespace Flux.Transcoder.Application.Users.Commands.Login;

public class LoginHandler
{
    // Хеш для несуществующего пользователя: проверка пароля идёт всегда, и по времени ответа
    // нельзя понять, есть ли такой логин.
    private static string? _dummyHash;

    public static async Task<LoginResult> HandleAsync(
        LoginCommand command,
        IReadRepository<User> users,
        IPasswordHasher hasher,
        IAuthTokenService tokens,
        CancellationToken ct)
    {
        var user = await users.FirstOrDefaultAsync(new UserByUsernameSpec(command.Username), ct);

        var hash = user?.PasswordHash ?? (_dummyHash ??= hasher.Hash(Guid.NewGuid().ToString()));
        var valid = hasher.Verify(command.Password, hash);

        if (user is null || !valid)
            throw new UnauthorizedException("Неверный логин или пароль.");

        var token = tokens.GenerateForUser(user.Id, user.Username, TimeSpan.FromHours(8));
        return new LoginResult(token);
    }
}
