using WaldauCastle.Models;

namespace WaldauCastle.Services;

public class NullTelegramNotificationService : ITelegramNotificationService
{
    public Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
