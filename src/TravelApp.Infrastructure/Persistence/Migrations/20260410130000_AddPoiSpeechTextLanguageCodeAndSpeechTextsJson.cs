using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class AddPoiSpeechTextLanguageCodeAndSpeechTextsJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SpeechTextLanguageCode",
            table: "POI",
            type: "nvarchar(10)",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SpeechTextsJson",
            table: "POI",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SpeechTextLanguageCode",
            table: "POI");

        migrationBuilder.DropColumn(
            name: "SpeechTextsJson",
            table: "POI");
    }
}
