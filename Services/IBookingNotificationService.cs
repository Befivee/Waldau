using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface IBookingNotificationService
{
    /// <summary>Ставит заявку в очередь уведомлений (Telegram + VK), не блокируя HTTP-ответ сайта.</summary>
    void ScheduleNewBookingNotification(Booking booking);
}
