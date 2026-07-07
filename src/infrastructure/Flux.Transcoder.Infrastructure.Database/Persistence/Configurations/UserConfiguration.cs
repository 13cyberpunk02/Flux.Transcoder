using Flux.Transcoder.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Username).HasColumnName("username").HasMaxLength(64).IsRequired();
        b.HasIndex(u => u.Username).IsUnique();
        b.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        b.Ignore(u => u.DomainEvents);
    }
}