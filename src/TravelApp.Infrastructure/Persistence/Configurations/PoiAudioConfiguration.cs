using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class PoiAudioConfiguration : IEntityTypeConfiguration<PoiAudio>
{
    public void Configure(EntityTypeBuilder<PoiAudio> builder)
    {
        builder.ToTable("Audio");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.AudioUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.Transcript)
            .HasMaxLength(4000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.PoiId, x.LanguageCode });
    }
}
