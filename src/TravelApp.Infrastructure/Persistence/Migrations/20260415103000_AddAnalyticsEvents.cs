using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class AddAnalyticsEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AnalyticsEvents",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EventType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                GuestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                DeviceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                PoiId = table.Column<int>(type: "int", nullable: true),
                TourId = table.Column<int>(type: "int", nullable: true),
                MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AnalyticsEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsEvents_OccurredAtUtc",
            table: "AnalyticsEvents",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsEvents_EventType_OccurredAtUtc",
            table: "AnalyticsEvents",
            columns: new[] { "EventType", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsEvents_PoiId_OccurredAtUtc",
            table: "AnalyticsEvents",
            columns: new[] { "PoiId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsEvents_TourId_OccurredAtUtc",
            table: "AnalyticsEvents",
            columns: new[] { "TourId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsEvents_Source_OccurredAtUtc",
            table: "AnalyticsEvents",
            columns: new[] { "Source", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AnalyticsEvents");
    }
}
