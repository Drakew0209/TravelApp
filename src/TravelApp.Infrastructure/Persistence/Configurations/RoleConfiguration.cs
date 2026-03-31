using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelApp.Domain.Entities;

namespace TravelApp.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(255);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new Role { Id = 1, Name = "SuperAdmin", Description = "System super administrator" },
            new Role { Id = 2, Name = "Admin", Description = "System administrator" },
            new Role { Id = 3, Name = "Owner", Description = "POI owner" },
            new Role { Id = 4, Name = "User", Description = "End user" }
        );
    }
}
