using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class PoiLocalizationConfiguration : IEntityTypeConfiguration<PoiLocalization>
{
    public void Configure(EntityTypeBuilder<PoiLocalization> builder)
    {
        builder.ToTable("POI_Localization");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Subtitle)
            .HasMaxLength(512);

        builder.Property(x => x.Description)
            .HasMaxLength(4000);

        builder.HasIndex(x => new { x.PoiId, x.LanguageCode })
            .IsUnique();
    }
}
