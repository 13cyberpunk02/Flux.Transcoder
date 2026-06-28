using System.Reflection;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Flux.Transcoder.Infrastructure.Database.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<EncodingProfile> EncodingProfiles => Set<EncodingProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}