using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface IBookingNotificationService
{
    Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
