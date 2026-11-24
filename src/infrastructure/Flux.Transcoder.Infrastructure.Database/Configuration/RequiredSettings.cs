using Microsoft.Extensions.Configuration;

namespace Flux.Transcoder.Infrastructure.Database.Configuration;

/// <summary>
/// Секреты не хранятся в репозитории. Если параметр не задан, сервис не стартует
/// и говорит, где его задать, вместо невнятной ошибки подключения позже.
/// </summary>
public static class RequiredSettings
{
    public const string Database = "ConnectionStrings:flux_transcoder_db";
    public const string RabbitMq = "ConnectionStrings:RabbitMq";
    public const string JwtSigningKey = "Jwt:SigningKey";
    public const string AdminPassword = "Seed:AdminPassword";

    /// <summary>HS256 требует ключ не короче 256 бит.</summary>
    public const int MinJwtKeyBytes = 32;

    public static string GetRequired(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        throw new InvalidOperationException(
            $"Не задан параметр '{key}'. " +
            $"Локально: dotnet user-secrets set \"{key}\" \"<значение>\" --project <проект>. " +
            $"На сервере: переменная окружения {ToEnvironmentVariable(key)}. Подробнее в README.");
    }

    public static string ToEnvironmentVariable(string key) => key.Replace(":", "__");
}
