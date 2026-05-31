using Microsoft.Extensions.Options;
using WaldauCastle.Models;
using WaldauCastle.Options;

namespace WaldauCastle.Services.VK;

public class VKNotificationService(
    VKApiClient apiClient,
    IOptions<VKOptions> options,
    ILogger<VKNotificationService> logger) : IVKNotificationService
{
    public async Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        if (!options.Value.TryGetAdminUserId(out var adminUserId))
        {
            logger.LogWarning("VK-уведомления отключены: укажите VK:AdminUserId.");
            return;
        }

        var culture = new System.Globalization.CultureInfo("ru-RU");
        var text =
            "🔔 Новая заявка\n\n" +
            $"ФИО: {booking.FullName}\n" +
            $"Телефон: {booking.Phone}\n" +
            $"Дата: {booking.VisitDate.ToString("d MMMM yyyy", culture)}\n" +
            $"Количество человек: {booking.PersonsCount}";

        try
        {
            await apiClient.SendMessageAsync(adminUserId, text, cancellationToken);
            logger.LogInformation("VK-уведомление о заявке отправлено admin {AdminUserId}.", adminUserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отправить уведомление о заявке в VK.");
        }
    }
}
