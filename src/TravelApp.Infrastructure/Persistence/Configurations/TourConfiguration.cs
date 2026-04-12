using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable("Tours");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AnchorPoiId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.CoverImageUrl)
            .HasMaxLength(1024);

        builder.Property(x => x.PrimaryLanguage)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.AnchorPoiId).IsUnique();

        builder.HasOne(x => x.AnchorPoi)
            .WithMany()
            .HasForeignKey(x => x.AnchorPoiId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
