using domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder builder)
    {     
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
