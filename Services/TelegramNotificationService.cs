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
        var adminChatIds = options.Value.GetAdminChatIds();
        if (adminChatIds.Count == 0)
            return;

        var culture = new System.Globalization.CultureInfo("ru-RU");
        var schedule = string.IsNullOrWhiteSpace(booking.VisitTime)
            ? booking.VisitDate.ToString("d MMMM yyyy", culture)
            : $"{booking.VisitDate.ToString("d MMMM yyyy", culture)}, {booking.VisitTime}";

        var text =
            "🔔 Новая заявка\n\n" +
            $"Экскурсия: {booking.ExcursionTitle}\n" +
            $"ФИО: {booking.FullName}\n" +
            $"Телефон: {booking.Phone}\n" +
            $"Дата: {schedule}\n" +
            $"Количество человек: {booking.PersonsCount}";

        foreach (var chatId in adminChatIds)
        {
            try
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.None,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось отправить уведомление о заявке в Telegram (chat {ChatId}).", chatId);
            }
        }
    }
}
