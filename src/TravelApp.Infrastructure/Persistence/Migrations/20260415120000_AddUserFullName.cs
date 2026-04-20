using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class AddUserFullName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FullName",
            table: "Users",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql("UPDATE [Users] SET [FullName] = [UserName] WHERE [FullName] = '' OR [FullName] IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FullName",
            table: "Users");
    }
}
