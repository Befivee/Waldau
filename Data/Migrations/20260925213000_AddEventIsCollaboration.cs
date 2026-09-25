using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaldauCastle.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260925213000_AddEventIsCollaboration")]
public partial class AddEventIsCollaboration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsCollaboration",
            table: "Events",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsCollaboration",
            table: "Events");
    }
}
