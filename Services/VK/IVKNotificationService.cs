using WaldauCastle.Models;

namespace WaldauCastle.Services.VK;

public interface IVKNotificationService
{
    Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default);
}
