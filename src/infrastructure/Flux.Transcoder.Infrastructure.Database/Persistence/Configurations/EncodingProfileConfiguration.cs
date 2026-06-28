using Flux.Transcoder.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Configurations;

public sealed class EncodingProfileConfiguration : IEntityTypeConfiguration<EncodingProfile>
{
    public void Configure(EntityTypeBuilder<EncodingProfile> builder)
    {
        builder.ToTable("encoding_profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        // коллекция value object Rendition -> owned-таблица
        builder.OwnsMany(p => p.Renditions, rb =>
        {
            rb.ToTable("renditions");
            rb.WithOwner().HasForeignKey("profile_id");
            rb.Property<int>("Id");           // суррогатный ключ owned-строки
            rb.HasKey("Id");

            rb.Property(r => r.Width).HasColumnName("width");
            rb.Property(r => r.Height).HasColumnName("height");
            rb.Property(r => r.BitrateKbps).HasColumnName("bitrate_kbps");
            rb.Property(r => r.Codec)
                .HasConversion<string>()
                .HasColumnName("codec")
                .HasMaxLength(16);
        });

        builder.Ignore(p => p.DomainEvents);
    }
}