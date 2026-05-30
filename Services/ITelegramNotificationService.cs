using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface ITelegramNotificationService
{
    Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
