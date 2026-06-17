using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface IBookingNotificationService
{
    /// <summary>Отправляет уведомления в фоне, не блокируя HTTP-ответ сайта.</summary>
    void ScheduleNewBookingNotification(Booking booking);

    Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
