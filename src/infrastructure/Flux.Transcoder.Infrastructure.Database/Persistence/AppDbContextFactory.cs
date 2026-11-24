using Flux.Transcoder.Infrastructure.Database.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Flux.Transcoder.Infrastructure.Database.Persistence;

/// <summary>Для dotnet ef: строка подключения из переменной окружения, в репозитории её нет.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var variable = RequiredSettings.ToEnvironmentVariable(RequiredSettings.Database);
        var connection = Environment.GetEnvironmentVariable(variable)
                         ?? Environment.GetEnvironmentVariable("flux_transcoder_db")
                         ?? throw new InvalidOperationException(
                             $"Для dotnet ef задайте строку подключения в переменной окружения {variable}.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new AppDbContext(options);
    }
}
