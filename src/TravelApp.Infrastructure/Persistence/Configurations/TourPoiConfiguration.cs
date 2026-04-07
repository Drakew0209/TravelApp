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

        builder.HasData(
            new TourPoi { Id = 1, TourId = 1, PoiId = 1, SortOrder = 1, DistanceFromPreviousMeters = 0 },
            new TourPoi { Id = 2, TourId = 1, PoiId = 2, SortOrder = 2, DistanceFromPreviousMeters = 900 },
            new TourPoi { Id = 3, TourId = 1, PoiId = 3, SortOrder = 3, DistanceFromPreviousMeters = 1100 },
            new TourPoi { Id = 4, TourId = 2, PoiId = 4, SortOrder = 1, DistanceFromPreviousMeters = 0 },
            new TourPoi { Id = 5, TourId = 2, PoiId = 5, SortOrder = 2, DistanceFromPreviousMeters = 300 },
            new TourPoi { Id = 6, TourId = 2, PoiId = 6, SortOrder = 3, DistanceFromPreviousMeters = 500 }
        );
    }
}
