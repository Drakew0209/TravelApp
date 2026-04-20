using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class AddUserLibrary : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserBookmarks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PoiId = table.Column<int>(type: "int", nullable: false),
                SavedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserBookmarks", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserBookmarks_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserHistoryEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PoiId = table.Column<int>(type: "int", nullable: false),
                VisitedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserHistoryEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserHistoryEntries_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserBookmarks_UserId_PoiId",
            table: "UserBookmarks",
            columns: new[] { "UserId", "PoiId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserBookmarks_UserId_SavedAtUtc",
            table: "UserBookmarks",
            columns: new[] { "UserId", "SavedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_UserHistoryEntries_UserId_PoiId",
            table: "UserHistoryEntries",
            columns: new[] { "UserId", "PoiId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserHistoryEntries_UserId_VisitedAtUtc",
            table: "UserHistoryEntries",
            columns: new[] { "UserId", "VisitedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserBookmarks");
        migrationBuilder.DropTable(name: "UserHistoryEntries");
    }
}
