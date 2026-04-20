using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class AnalyticsEventConfiguration : IEntityTypeConfiguration<AnalyticsEvent>
{
    public void Configure(EntityTypeBuilder<AnalyticsEvent> builder)
    {
        builder.ToTable("AnalyticsEvents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(16).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(128);
        builder.Property(x => x.GuestId).HasMaxLength(128);
        builder.Property(x => x.DeviceId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SessionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MetadataJson);

        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.EventType, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.PoiId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.TourId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.Source, x.OccurredAtUtc });
    }
}
