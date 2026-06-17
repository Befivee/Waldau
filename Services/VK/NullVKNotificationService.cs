using WaldauCastle.Models;

namespace WaldauCastle.Services.VK;

public class NullVKNotificationService : IVKNotificationService
{
    public Task<bool> NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
