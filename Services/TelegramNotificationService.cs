using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Models;
using WaldauCastle.Options;

namespace WaldauCastle.Services;

public class TelegramNotificationService(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    public async Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var telegramOptions = options.Value;
        if (!telegramOptions.TryGetAdminChatId(out var adminChatId))
            return;

        var culture = new System.Globalization.CultureInfo("ru-RU");
        var text =
            "🔔 Новая заявка\n\n" +
            $"Имя: {booking.FullName}\n" +
            $"Телефон: {booking.Phone}\n" +
            $"Дата: {booking.VisitDate.ToString("d MMMM yyyy", culture)}\n" +
            $"Количество человек: {booking.PersonsCount}";

        try
        {
            await botClient.SendMessage(
                chatId: adminChatId,
                text: text,
                parseMode: ParseMode.None,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отправить уведомление о заявке в Telegram.");
        }
    }
}
