using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class TourPoiConfiguration : IEntityTypeConfiguration<TourPoi>
{
    public void Configure(EntityTypeBuilder<TourPoi> builder)
    {
        builder.ToTable("TourPois");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.DistanceFromPreviousMeters);

        builder.HasIndex(x => new { x.TourId, x.SortOrder }).IsUnique();

        builder.HasOne(x => x.Tour)
            .WithMany(x => x.TourPois)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Poi)
            .WithMany()
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
