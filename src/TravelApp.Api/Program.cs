using TravelApp.Application;
using TravelApp.Infrastructure;
using TravelApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddApplication();

var jwtSecret = GetRequiredConfigValue(builder.Configuration, "Jwt:Secret");
var jwtIssuer = GetRequiredConfigValue(builder.Configuration, "Jwt:Issuer");
var jwtAudience = GetRequiredConfigValue(builder.Configuration, "Jwt:Audience");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("TravelAppDb")
    ?? throw new InvalidOperationException("Missing connection string 'TravelAppDb'.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TravelAppDbContext>();

    if (ShouldBaselineLegacyDatabase(dbContext))
    {
        SeedLegacyMigrationHistory(dbContext);
    }

    dbContext.Database.Migrate();
    await EnsureTourSchemaAsync(dbContext);
    await EnsureUserSchemaAsync(dbContext);
    await EnsureRefreshTokenSchemaAsync(dbContext);
    await EnsurePoiSpeechTextColumnAsync(dbContext);
    await EnsurePoiSpeechTextsColumnAsync(dbContext);
    await EnsurePoiSpeechTextLanguageCodeColumnAsync(dbContext);
    await EnsureAnalyticsSchemaAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "OK",
    Service = "TravelApp.Api"
}));

static bool ShouldBaselineLegacyDatabase(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var poiCommand = connection.CreateCommand();
        poiCommand.CommandText = "SELECT OBJECT_ID(N'[POI]')";
        var poiObjectId = poiCommand.ExecuteScalar();

        if (poiObjectId is null or DBNull)
        {
            return false;
        }

        using var historyCommand = connection.CreateCommand();
        historyCommand.CommandText = "SELECT CASE WHEN OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM [__EFMigrationsHistory]) END";
        return Convert.ToInt32(historyCommand.ExecuteScalar()) == 0;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static async Task EnsureRefreshTokenSchemaAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = "SELECT OBJECT_ID(N'[RefreshTokens]')";
        var tableObjectId = await tableCommand.ExecuteScalarAsync();

        if (tableObjectId is null or DBNull)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                CREATE TABLE [RefreshTokens] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [TokenHash] nvarchar(128) NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [ExpiresAtUtc] datetimeoffset NOT NULL,
                    [RevokedAtUtc] datetimeoffset NULL,
                    [ReplacedByTokenHash] nvarchar(128) NULL,
                    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                );
                """);
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_TokenHash' AND object_id = OBJECT_ID(N'[RefreshTokens]'))
                CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId_ExpiresAtUtc' AND object_id = OBJECT_ID(N'[RefreshTokens]'))
                CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAtUtc] ON [RefreshTokens] ([UserId], [ExpiresAtUtc]);

            IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260410180000_AddRefreshTokens')
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260410180000_AddRefreshTokens', '10.0.0');
            """);
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsureUserSchemaAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FullName'";
        var exists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE [Users] ADD [FullName] nvarchar(200) NOT NULL CONSTRAINT [DF_Users_FullName] DEFAULT('');");
        }

        await dbContext.Database.ExecuteSqlRawAsync("UPDATE [Users] SET [FullName] = [UserName] WHERE [FullName] = '' OR [FullName] IS NULL;");
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsurePoiSpeechTextsColumnAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'POI' AND COLUMN_NAME = 'SpeechTextsJson'";
        var exists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE [POI] ADD [SpeechTextsJson] nvarchar(max) NULL;");
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static string GetRequiredConfigValue(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required configuration value '{key}'.");
    }

    return value;
}

static void SeedLegacyMigrationHistory(TravelAppDbContext dbContext)
{
    dbContext.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
        BEGIN
            CREATE TABLE [__EFMigrationsHistory] (
                [MigrationId] nvarchar(150) NOT NULL,
                [ProductVersion] nvarchar(32) NOT NULL,
                CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
            );
        END;

        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260331040844_InitialCreate')
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260331040844_InitialCreate', '10.0.0');

        IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260406190000_AddToursAndTourPois')
            INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260406190000_AddToursAndTourPois', '10.0.0');
        """);
}

static async Task EnsureTourSchemaAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var tourCommand = connection.CreateCommand();
        tourCommand.CommandText = "SELECT OBJECT_ID(N'[Tours]')";
        var toursObjectId = await tourCommand.ExecuteScalarAsync();

        using var tourPoisCommand = connection.CreateCommand();
        tourPoisCommand.CommandText = "SELECT OBJECT_ID(N'[TourPois]')";
        var tourPoisObjectId = await tourPoisCommand.ExecuteScalarAsync();

        if (toursObjectId is not null and not DBNull && tourPoisObjectId is not null and not DBNull)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Tours]') IS NULL
            BEGIN
                CREATE TABLE [Tours] (
                    [Id] int NOT NULL IDENTITY,
                    [AnchorPoiId] int NOT NULL,
                    [Name] nvarchar(256) NOT NULL,
                    [Description] nvarchar(4000) NOT NULL,
                    [CoverImageUrl] nvarchar(1024) NULL,
                    [PrimaryLanguage] nvarchar(10) NOT NULL,
                    [IsPublished] bit NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    CONSTRAINT [PK_Tours] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Tours_POI_AnchorPoiId] FOREIGN KEY ([AnchorPoiId]) REFERENCES [POI] ([Id]) ON DELETE NO ACTION
                );
            END;

            IF OBJECT_ID(N'[TourPois]') IS NULL
            BEGIN
                CREATE TABLE [TourPois] (
                    [Id] int NOT NULL IDENTITY,
                    [TourId] int NOT NULL,
                    [PoiId] int NOT NULL,
                    [SortOrder] int NOT NULL,
                    [DistanceFromPreviousMeters] float NULL,
                    CONSTRAINT [PK_TourPois] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_TourPois_POI_PoiId] FOREIGN KEY ([PoiId]) REFERENCES [POI] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_TourPois_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [Tours] ([Id]) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [Tours] WHERE [Id] IN (1, 2))
            BEGIN
                SET IDENTITY_INSERT [Tours] ON;
                INSERT INTO [Tours] ([Id], [AnchorPoiId], [Name], [Description], [CoverImageUrl], [PrimaryLanguage], [IsPublished], [CreatedAtUtc], [UpdatedAtUtc])
                VALUES
                    (1, 1, N'HCM Food Tour', N'Tour ẩm thực Sài Gòn với các điểm dừng được sắp xếp theo lộ trình thật.', N'https://placehold.co/1200x800/png?text=HCM+Food+Tour', N'vi', 1, '2025-01-01T00:00:00+00:00', NULL),
                    (2, 4, N'Hanoi Food Tour', N'Tour ẩm thực Hà Nội với các mốc waypoint, bản đồ và audio tự động.', N'https://placehold.co/1200x800/png?text=Hanoi+Food+Tour', N'vi', 1, '2025-01-01T00:00:00+00:00', NULL);
                SET IDENTITY_INSERT [Tours] OFF;
            END;

            IF NOT EXISTS (SELECT 1 FROM [TourPois] WHERE [Id] IN (1, 2, 3, 4, 5, 6))
            BEGIN
                SET IDENTITY_INSERT [TourPois] ON;
                INSERT INTO [TourPois] ([Id], [TourId], [PoiId], [SortOrder], [DistanceFromPreviousMeters])
                VALUES
                    (1, 1, 1, 1, 0),
                    (2, 1, 2, 2, 900),
                    (3, 1, 3, 3, 1100),
                    (4, 2, 4, 1, 0),
                    (5, 2, 5, 2, 300),
                    (6, 2, 6, 3, 500);
                SET IDENTITY_INSERT [TourPois] OFF;
            END;

            IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260406190000_AddToursAndTourPois')
                INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260406190000_AddToursAndTourPois', '10.0.0');
            """, cancellationToken: default);
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsurePoiSpeechTextColumnAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'POI' AND COLUMN_NAME = 'SpeechText'";
        var exists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE [POI] ADD [SpeechText] nvarchar(4000) NULL;");
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsurePoiSpeechTextLanguageCodeColumnAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'POI' AND COLUMN_NAME = 'SpeechTextLanguageCode'";
        var exists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE [POI] ADD [SpeechTextLanguageCode] nvarchar(10) NULL;");
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task EnsureAnalyticsSchemaAsync(TravelAppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT OBJECT_ID(N'[AnalyticsEvents]')";
        var exists = await command.ExecuteScalarAsync();

        if (exists is null or DBNull)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                CREATE TABLE [AnalyticsEvents] (
                    [Id] bigint NOT NULL IDENTITY,
                    [OccurredAtUtc] datetimeoffset NOT NULL,
                    [EventType] nvarchar(32) NOT NULL,
                    [Source] nvarchar(16) NOT NULL,
                    [UserId] nvarchar(128) NULL,
                    [GuestId] nvarchar(128) NULL,
                    [DeviceId] nvarchar(128) NOT NULL,
                    [SessionId] nvarchar(128) NOT NULL,
                    [PoiId] int NULL,
                    [TourId] int NULL,
                    [MetadataJson] nvarchar(max) NULL,
                    CONSTRAINT [PK_AnalyticsEvents] PRIMARY KEY ([Id])
                );
                """);
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AnalyticsEvents_OccurredAtUtc' AND object_id = OBJECT_ID(N'[AnalyticsEvents]'))
                CREATE INDEX [IX_AnalyticsEvents_OccurredAtUtc] ON [AnalyticsEvents] ([OccurredAtUtc]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AnalyticsEvents_EventType_OccurredAtUtc' AND object_id = OBJECT_ID(N'[AnalyticsEvents]'))
                CREATE INDEX [IX_AnalyticsEvents_EventType_OccurredAtUtc] ON [AnalyticsEvents] ([EventType], [OccurredAtUtc]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AnalyticsEvents_PoiId_OccurredAtUtc' AND object_id = OBJECT_ID(N'[AnalyticsEvents]'))
                CREATE INDEX [IX_AnalyticsEvents_PoiId_OccurredAtUtc] ON [AnalyticsEvents] ([PoiId], [OccurredAtUtc]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AnalyticsEvents_TourId_OccurredAtUtc' AND object_id = OBJECT_ID(N'[AnalyticsEvents]'))
                CREATE INDEX [IX_AnalyticsEvents_TourId_OccurredAtUtc] ON [AnalyticsEvents] ([TourId], [OccurredAtUtc]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AnalyticsEvents_Source_OccurredAtUtc' AND object_id = OBJECT_ID(N'[AnalyticsEvents]'))
                CREATE INDEX [IX_AnalyticsEvents_Source_OccurredAtUtc] ON [AnalyticsEvents] ([Source], [OccurredAtUtc]);
            """);
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

app.Run();
