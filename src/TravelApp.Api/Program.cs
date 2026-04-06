using TravelApp.Application;
using TravelApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddApplication();

var connectionString = builder.Configuration.GetConnectionString("TravelAppDb")
    ?? throw new InvalidOperationException("Missing connection string 'TravelAppDb'.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "OK",
    Service = "TravelApp.Api"
}));

app.Run();
