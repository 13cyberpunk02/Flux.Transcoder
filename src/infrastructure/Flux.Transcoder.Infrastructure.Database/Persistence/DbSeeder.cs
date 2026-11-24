using System.Security.Cryptography;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Users;
using Flux.Transcoder.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flux.Transcoder.Infrastructure.Database.Persistence;

/// <summary>
/// Первый пользователь admin. Пароль берётся из Seed:AdminPassword; если он не задан,
/// генерируется случайный и печатается в лог один раз. Пароль admin/admin, созданный
/// прежними версиями, заменяется заданным.
/// </summary>
public static class DbSeeder
{
    private const string AdminName = "admin";
    private const string LegacyPassword = "admin";

    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

        var configured = configuration[RequiredSettings.AdminPassword];
        var admin = await repo.FirstOrDefaultAsync(new UserByUsernameSpec(AdminName));

        if (admin is null)
        {
            var password = string.IsNullOrWhiteSpace(configured) ? Generate() : configured;
            await repo.AddAsync(User.Create(AdminName, hasher.Hash(password)));
            await uow.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(configured))
                logger.LogWarning(
                    // :l — без кавычек, иначе пароль копируют из лога вместе с ними
                    "Создан пользователь {User} со случайным паролем {Password:l}. Он показывается один раз: " +
                    "сохраните его или задайте свой через {Setting} до первого запуска.",
                    AdminName, password, RequiredSettings.AdminPassword);
            return;
        }

        if (!hasher.Verify(LegacyPassword, admin.PasswordHash))
            return;

        if (string.IsNullOrWhiteSpace(configured))
        {
            logger.LogWarning(
                "У пользователя {User} пароль по умолчанию. Задайте новый через {Setting} и перезапустите API.",
                AdminName, RequiredSettings.AdminPassword);
            return;
        }

        admin.ChangePassword(hasher.Hash(configured));
        await uow.SaveChangesAsync();
        logger.LogInformation("Пароль по умолчанию пользователя {User} заменён на заданный в {Setting}.",
            AdminName, RequiredSettings.AdminPassword);
    }

    private static string Generate()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        return RandomNumberGenerator.GetString(alphabet, 20);
    }
}
