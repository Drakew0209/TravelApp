using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class UserHistoryEntryConfiguration : IEntityTypeConfiguration<UserHistoryEntry>
{
    public void Configure(EntityTypeBuilder<UserHistoryEntry> builder)
    {
        builder.ToTable("UserHistoryEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.VisitedAtUtc).IsRequired();
        builder.Property(x => x.PoiId).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PoiId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.VisitedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserHistoryEntries)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
