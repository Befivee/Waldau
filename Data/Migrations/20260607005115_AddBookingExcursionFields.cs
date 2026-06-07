using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaldauCastle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingExcursionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExcursionKind",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExcursionTitle",
                table: "Bookings",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitTime",
                table: "Bookings",
                type: "TEXT",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcursionKind",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExcursionTitle",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VisitTime",
                table: "Bookings");
        }
    }
}
