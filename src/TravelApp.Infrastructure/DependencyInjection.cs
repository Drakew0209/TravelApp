using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TravelApp.Application.Abstractions.Auth;
using TravelApp.Application.Abstractions.Analytics;
using TravelApp.Application.Abstractions.Library;
using TravelApp.Application.Abstractions.Persistence;
using TravelApp.Application.Abstractions.Pois;
using TravelApp.Application.Abstractions.Users;
using TravelApp.Application.Abstractions.Tours;
using TravelApp.Infrastructure.Persistence;
using TravelApp.Infrastructure.Services.Analytics;
using TravelApp.Infrastructure.Services.Auth;
using TravelApp.Infrastructure.Services.Library;
using TravelApp.Infrastructure.Services.Pois;
using TravelApp.Infrastructure.Services.Users;
using TravelApp.Infrastructure.Services.Tours;

namespace TravelApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TravelAppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<ITravelAppDbContext>(provider => provider.GetRequiredService<TravelAppDbContext>());
        services.AddScoped<IAnalyticsTrackingService, AnalyticsService>();
        services.AddScoped<IAnalyticsDashboardService, AnalyticsService>();
        services.AddScoped<IUserLibraryService, UserLibraryService>();
        services.AddScoped<IPoiQueryService, PoiQueryService>();
        services.AddScoped<ITourQueryService, TourQueryService>();
        services.AddScoped<ITourAdminService, TourAdminService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
