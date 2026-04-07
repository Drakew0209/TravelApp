using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TravelApp.Application.Abstractions.Auth;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Application.Abstractions.Tours;
using TravelApp.Infrastructure.Persistence;
using TravelApp.Infrastructure.Services.Auth;
using TravelApp.Infrastructure.Services.Pois;
using TravelApp.Infrastructure.Services.Tours;

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
        services.AddScoped<ITourQueryService, TourQueryService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
