using Flux.Transcoder.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Flux.Transcoder.Infrastructure.Database.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private sealed class Dummy;
    private readonly PasswordHasher<Dummy> _inner = new();
    private static readonly Dummy _dummy = new();

    public string Hash(string password) =>
        _inner.HashPassword(_dummy, password);

    public bool Verify(string password, string hash)
    {
        var result = _inner.VerifyHashedPassword(_dummy, hash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}