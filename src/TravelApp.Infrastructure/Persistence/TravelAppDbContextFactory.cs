using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelApp.Infrastructure.Persistence;

public class TravelAppDbContextFactory : IDesignTimeDbContextFactory<TravelAppDbContext>
{
    public TravelAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TravelAppDbContext>();
        const string fallbackConnection = "Server=(localdb)\\MSSQLLocalDB;Database=TravelAppDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(fallbackConnection);

        return new TravelAppDbContext(optionsBuilder.Options);
    }
}
