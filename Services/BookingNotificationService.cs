using WaldauCastle.Models;
using WaldauCastle.Services.VK;

namespace WaldauCastle.Services;

public class BookingNotificationService(
    ITelegramNotificationService telegram,
    IVKNotificationService vk,
    ILogger<BookingNotificationService> logger) : IBookingNotificationService
{
    public async Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        try
        {
            await telegram.NotifyNewBookingAsync(booking, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка Telegram-уведомления о заявке.");
        }

        try
        {
            await vk.NotifyNewBookingAsync(booking, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка VK-уведомления о заявке.");
        }
    }
}
