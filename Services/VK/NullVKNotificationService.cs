using WaldauCastle.Models;

namespace WaldauCastle.Services.VK;

public class NullVKNotificationService : IVKNotificationService
{
    public Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
