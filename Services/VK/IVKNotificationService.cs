using WaldauCastle.Models;

namespace WaldauCastle.Services.VK;

public interface IVKNotificationService
{
    /// <returns>true, если уведомление отправлено или VK отключён.</returns>
    Task<bool> NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
