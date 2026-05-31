using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaldauCastle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExcursionImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Excursions",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Excursions");
        }
    }
}
