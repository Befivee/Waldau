using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface ITelegramNotificationService
{
    /// <returns>true, если уведомление отправлено или Telegram отключён.</returns>
    Task<bool> NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
