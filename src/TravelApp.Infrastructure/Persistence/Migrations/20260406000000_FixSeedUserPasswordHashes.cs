using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class FixSeedUserPasswordHashes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var demoPasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123456");
        var khanhPasswordHash = BCrypt.Net.BCrypt.HashPassword("Khanh@123456");
        var guestPasswordHash = BCrypt.Net.BCrypt.HashPassword("Guest@123456");

        migrationBuilder.UpdateData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("550e8400-e29b-41d4-a716-446655440001"),
            column: "PasswordHash",
            value: demoPasswordHash);

        migrationBuilder.UpdateData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("550e8400-e29b-41d4-a716-446655440002"),
            column: "PasswordHash",
            value: khanhPasswordHash);

        migrationBuilder.UpdateData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("550e8400-e29b-41d4-a716-446655440003"),
            column: "PasswordHash",
            value: guestPasswordHash);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("550e8400-e29b-41d4-a716-446655440001"),
            column: "PasswordHash",
            value: "$2a$11$G3FqI3a5BM5rMX2hy6vJCOLnXK.VsH8S0SzS9L5KJ5UqGW8E5K3W2");

        migrationBuilder.UpdateData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("550e8400-e29b-41d4-a716-446655440002"),
            column: "PasswordHash",
            value: "$2a$11$F8K7L9M1N2O3P4Q5R6S7T8U9V0W1X2Y3Z4A5B6C7D8E9F0G1H2I3");

        migrationBuilder.UpdateData(
            table: "Users",
            keyColumn: "Id",
            keyValue: new Guid("550e8400-e29b-41d4-a716-446655440003"),
            column: "PasswordHash",
            value: "$2a$11$J9K8L7M6N5O4P3Q2R1S0T9U8V7W6X5Y4Z3A2B1C0D9E8F7G6H5I4");
    }
}
