using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Flux.Transcoder.Infrastructure.Database.Persistence;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var existing = await repo.FirstOrDefaultAsync(new UserByUsernameSpec("admin"));
        if (existing is not null)
            return;

        var admin = User.Create("admin", hasher.Hash("admin"));
        await repo.AddAsync(admin);
        await uow.SaveChangesAsync();
    }
}