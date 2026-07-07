using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Domain.Users;

public sealed class User : AggregateRoot
{
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }

    private User() { Username = string.Empty; PasswordHash = string.Empty; }  // для EF

    private User(Guid id, string username, string passwordHash) : base(id)
    {
        Username = username;
        PasswordHash = passwordHash;
    }

    public static User Create(string username, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Имя пользователя не может быть пустым.");
        return string.IsNullOrWhiteSpace(passwordHash) ? 
            throw new DomainException("Хеш пароля обязателен.") : 
            new User(Guid.NewGuid(), username.Trim(), passwordHash);
    }

    public void ChangePassword(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new DomainException("Хеш пароля обязателен.");
        PasswordHash = newHash;
    }
}