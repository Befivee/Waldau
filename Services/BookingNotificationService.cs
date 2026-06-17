using WaldauCastle.Models;

namespace WaldauCastle.Services;

public class BookingNotificationService(
    BookingNotificationQueue queue,
    ILogger<BookingNotificationService> logger) : IBookingNotificationService
{
    public void ScheduleNewBookingNotification(Booking booking)
    {
        var snapshot = CloneBooking(booking);

        if (queue.TryEnqueue(snapshot))
        {
            logger.LogInformation(
                "Заявка #{BookingId} поставлена в очередь уведомлений (Telegram + VK).",
                snapshot.Id);
            return;
        }

        logger.LogError(
            "Не удалось поставить заявку #{BookingId} в очередь уведомлений.",
            snapshot.Id);
    }

    private static Booking CloneBooking(Booking booking) =>
        new()
        {
            Id = booking.Id,
            FullName = booking.FullName,
            Phone = booking.Phone,
            VisitDate = booking.VisitDate,
            ExcursionKind = booking.ExcursionKind,
            ExcursionTitle = booking.ExcursionTitle,
            VisitTime = booking.VisitTime,
            PersonsCount = booking.PersonsCount,
            CreatedAt = booking.CreatedAt,
            PersonalDataConsent = booking.PersonalDataConsent
        };
}
