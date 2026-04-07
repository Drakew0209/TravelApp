using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SeedUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Insert demo users for testing
        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "UserName", "Email", "PasswordHash", "IsActive", "CreatedAtUtc" },
            values: new object[,]
            {
                {
                    new Guid("550e8400-e29b-41d4-a716-446655440001"),
                    "demo_user",
                    "demo@example.com",
                    // Password: Demo@123456 (hashed with BCrypt)
                    "$2a$11$G3FqI3a5BM5rMX2hy6vJCOLnXK.VsH8S0SzS9L5KJ5UqGW8E5K3W2",
                    true,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    new Guid("550e8400-e29b-41d4-a716-446655440002"),
                    "khanh_user",
                    "khanh@example.com",
                    // Password: Khanh@123456 (hashed with BCrypt)
                    "$2a$11$F8K7L9M1N2O3P4Q5R6S7T8U9V0W1X2Y3Z4A5B6C7D8E9F0G1H2I3",
                    true,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                {
                    new Guid("550e8400-e29b-41d4-a716-446655440003"),
                    "guest_user",
                    "guest@example.com",
                    // Password: Guest@123456 (hashed with BCrypt)
                    "$2a$11$J9K8L7M6N5O4P3Q2R1S0T9U8V7W6X5Y4Z3A2B1C0D9E8F7G6H5I4",
                    true,
                    new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
                }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Users",
            keyColumn: "Id",
            keyValues: new object[]
            {
                new Guid("550e8400-e29b-41d4-a716-446655440001"),
                new Guid("550e8400-e29b-41d4-a716-446655440002"),
                new Guid("550e8400-e29b-41d4-a716-446655440003")
            });
    }
}
