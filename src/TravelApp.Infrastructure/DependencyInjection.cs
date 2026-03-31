using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Infrastructure.Persistence;
using TravelApp.Infrastructure.Services.Pois;

namespace TravelApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TravelAppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<ITravelAppDbContext>(provider => provider.GetRequiredService<TravelAppDbContext>());
        services.AddScoped<IPoiQueryService, PoiQueryService>();

        return services;
    }
}
