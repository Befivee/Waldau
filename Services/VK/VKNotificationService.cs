using Microsoft.Extensions.Options;
using WaldauCastle.Models;
using WaldauCastle.Options;

namespace WaldauCastle.Services.VK;

public class VKNotificationService(
    VKApiClient apiClient,
    IOptions<VKOptions> options,
    ILogger<VKNotificationService> logger) : IVKNotificationService
{
    public async Task<bool> NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        if (!options.Value.TryGetAdminUserId(out var adminUserId))
        {
            logger.LogWarning("VK-уведомления отключены: укажите VK:AdminUserId.");
            return true;
        }

        var text = BookingNotificationText.Format(booking);

        try
        {
            await apiClient.SendMessageAsync(
                adminUserId,
                text,
                VKKeyboards.BackToMainMenu(),
                cancellationToken);
            logger.LogInformation("VK-уведомление о заявке #{BookingId} отправлено admin {AdminUserId}.", booking.Id, adminUserId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отправить уведомление о заявке #{BookingId} в VK.", booking.Id);
            return false;
        }
    }
}
