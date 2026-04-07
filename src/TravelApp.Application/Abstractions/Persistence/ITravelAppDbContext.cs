using Microsoft.EntityFrameworkCore;
using TravelApp.Domain.Entities;

namespace TravelApp.Application.Abstractions.Persistence;

public interface ITravelAppDbContext
{
    DbSet<Poi> Pois { get; }
    DbSet<PoiLocalization> PoiLocalizations { get; }
    DbSet<PoiAudio> PoiAudios { get; }
    DbSet<Tour> Tours { get; }
    DbSet<TourPoi> TourPois { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
