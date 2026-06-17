using WaldauCastle.Models;

namespace WaldauCastle.Services;

public class BookingNotificationService(
    BookingNotificationQueue queue,
    ILogger<BookingNotificationService> logger) : IBookingNotificationService
{
    public void ScheduleNewBookingNotification(Booking booking)
    {
        if (booking.Id <= 0)
        {
            logger.LogError("Не удалось поставить заявку в очередь уведомлений: отсутствует Id.");
            return;
        }

        if (queue.TryEnqueue(booking.Id))
        {
            logger.LogInformation(
                "Заявка #{BookingId} поставлена в очередь уведомлений (Telegram + VK).",
                booking.Id);
            return;
        }

        logger.LogError(
            "Не удалось поставить заявку #{BookingId} в очередь уведомлений.",
            booking.Id);
    }
}
