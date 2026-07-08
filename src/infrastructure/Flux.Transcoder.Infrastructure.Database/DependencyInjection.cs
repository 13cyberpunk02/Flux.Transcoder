using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Infrastructure.Database.Persistence;
using Flux.Transcoder.Infrastructure.Database.Persistence.Repositories;
using Flux.Transcoder.Infrastructure.Database.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flux.Transcoder.Infrastructure.Database;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContextPool<AppDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("flux_transcoder_db")));

        // Репозитории (открытые дженерики)
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // JWT
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        
        //Password hasher 
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        
        //Токен генератор для авторизации пользователя
        services.AddSingleton<IAuthTokenService, AuthTokenService>();

        return services;
    }
}