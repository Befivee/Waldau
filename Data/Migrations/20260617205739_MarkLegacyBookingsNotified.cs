using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaldauCastle.Data.Migrations
{
    /// <inheritdoc />
    public partial class MarkLegacyBookingsNotified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Bookings
                SET TelegramNotifiedAt = COALESCE(TelegramNotifiedAt, CreatedAt),
                    VkNotifiedAt = COALESCE(VkNotifiedAt, CreatedAt)
                WHERE TelegramNotifiedAt IS NULL OR VkNotifiedAt IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Bookings
                SET TelegramNotifiedAt = NULL,
                    VkNotifiedAt = NULL
                WHERE TelegramNotifiedAt = CreatedAt AND VkNotifiedAt = CreatedAt;
                """);
        }
    }
}
