using System.Text;
using Flux.Transcoder.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Flux.Transcoder.Infrastructure.Database.Security;

public sealed class AuthTokenService(IOptions<JwtOptions> options) : IAuthTokenService
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateForUser(Guid userId, string username, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime),
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.Name] = username,
                ["user_id"] = userId.ToString()
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}