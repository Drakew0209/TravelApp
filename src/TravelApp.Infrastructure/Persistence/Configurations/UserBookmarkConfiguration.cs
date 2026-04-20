using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class UserBookmarkConfiguration : IEntityTypeConfiguration<UserBookmark>
{
    public void Configure(EntityTypeBuilder<UserBookmark> builder)
    {
        builder.ToTable("UserBookmarks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SavedAtUtc).IsRequired();
        builder.Property(x => x.PoiId).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PoiId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.SavedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserBookmarks)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
