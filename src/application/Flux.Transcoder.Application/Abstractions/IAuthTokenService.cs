namespace Flux.Transcoder.Application.Abstractions;

public interface IAuthTokenService
{
    string GenerateForUser(Guid userId, string username, TimeSpan lifetime);
}