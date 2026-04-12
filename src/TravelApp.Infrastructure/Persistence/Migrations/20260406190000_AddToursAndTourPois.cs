using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class AddToursAndTourPois : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tours",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AnchorPoiId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CoverImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                PrimaryLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                IsPublished = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tours", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tours_POI_AnchorPoiId",
                    column: x => x.AnchorPoiId,
                    principalTable: "POI",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TourPois",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TourId = table.Column<int>(type: "int", nullable: false),
                PoiId = table.Column<int>(type: "int", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                DistanceFromPreviousMeters = table.Column<double>(type: "float", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TourPois", x => x.Id);
                table.ForeignKey(
                    name: "FK_TourPois_POI_PoiId",
                    column: x => x.PoiId,
                    principalTable: "POI",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TourPois_Tours_TourId",
                    column: x => x.TourId,
                    principalTable: "Tours",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Tours_AnchorPoiId",
            table: "Tours",
            column: "AnchorPoiId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TourPois_PoiId",
            table: "TourPois",
            column: "PoiId");

        migrationBuilder.CreateIndex(
            name: "IX_TourPois_TourId_SortOrder",
            table: "TourPois",
            columns: new[] { "TourId", "SortOrder" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TourPois");

        migrationBuilder.DropTable(
            name: "Tours");
    }
}
