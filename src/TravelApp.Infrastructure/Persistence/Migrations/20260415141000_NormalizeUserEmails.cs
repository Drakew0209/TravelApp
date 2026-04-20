using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelApp.Infrastructure.Persistence.Migrations;

public partial class NormalizeUserEmails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
UPDATE [Users]
SET [Email] = LOWER(LTRIM(RTRIM([Email])))
WHERE [Email] IS NOT NULL;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
