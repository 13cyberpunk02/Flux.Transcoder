using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Configurations;

public sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("channels");

        builder.HasKey(c => c.Id);

        // value object SourceUrl -> строковая колонка
        builder.Property(c => c.Source)
            .HasConversion(
                src => src.Value,
                value => new SourceUrl(value))
            .HasColumnName("source_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()          // enum -> текст
            .HasColumnName("status")
            .HasMaxLength(32)
            // статус пишут и API, и воркер: UPDATE проходит, только если он не менялся с момента чтения
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(c => c.RunRevision)
            .HasColumnName("run_revision")
            .IsConcurrencyToken()   // ловит и Failed -> Restart -> Starting, где одного статуса мало
            .IsRequired();

        builder.Ignore(c => c.IsInactive);

        // связь Channel -> EncodingProfile
        builder.HasOne(c => c.Profile)
            .WithMany()
            .HasForeignKey("profile_id")       // теневой FK
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.OwnsOne(c => c.Retention, r =>
        {
            r.Property(x => x.ArchiveRetention)
                .HasColumnName("archive_retention")
                .IsRequired();
        });
        
        // доменные события не персистятся
        builder.Ignore(c => c.DomainEvents);
    }
}