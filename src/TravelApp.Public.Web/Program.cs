using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using TravelApp.Public.Web.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var supportedCultures = new[]
{
    new CultureInfo("vi-VN"),
    new CultureInfo("en-US"),
    new CultureInfo("ja-JP"),
    new CultureInfo("de-DE")
};

builder.Services.AddLocalization();

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.Name = "TravelApp.Public.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });
builder.Services.AddAuthorization();
builder.Services.Configure<TravelAppApiOptions>(options =>
{
    var configured = builder.Configuration["TravelAppApi:BaseUrl"];
    var envConfigured = Environment.GetEnvironmentVariable("TRAVELAPP_API_BASE_URL");
    options.BaseUrl = !string.IsNullOrWhiteSpace(envConfigured)
        ? envConfigured.Trim().TrimEnd('/') + "/"
        : !string.IsNullOrWhiteSpace(configured)
            ? configured.Trim().TrimEnd('/') + "/"
            : options.BaseUrl;
});
builder.Services.AddHttpClient<ITravelAppPublicApiClient, TravelAppPublicApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TravelAppApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});
builder.Services.AddHttpClient<IPublicAuthApiClient, PublicAuthApiClient>();
builder.Services.AddScoped<PublicLibraryApiProxyService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi-VN"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
