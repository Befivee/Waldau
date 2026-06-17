using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaldauCastle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingNotificationTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TelegramNotifiedAt",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VkNotifiedAt",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TelegramNotifiedAt_VkNotifiedAt_CreatedAt",
                table: "Bookings",
                columns: new[] { "TelegramNotifiedAt", "VkNotifiedAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_TelegramNotifiedAt_VkNotifiedAt_CreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TelegramNotifiedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VkNotifiedAt",
                table: "Bookings");
        }
    }
}
