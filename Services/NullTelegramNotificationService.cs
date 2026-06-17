using WaldauCastle.Models;

namespace WaldauCastle.Services;

public class NullTelegramNotificationService : ITelegramNotificationService
{
    public Task<bool> NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
