using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Models;
using WaldauCastle.Options;
using WaldauCastle.Services.Telegram;

namespace WaldauCastle.Services;

public class TelegramNotificationService(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    public async Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var telegram = options.Value;
        if (!telegram.IsConfigured)
            return;

        var adminChatIds = telegram.GetAdminChatIds();
        if (adminChatIds.Count == 0)
            return;

        var text = BookingNotificationText.Format(booking);

        foreach (var chatId in adminChatIds)
        {
            await SendWithRetryAsync(chatId, text, booking.Id, cancellationToken);
        }
    }

    private async Task SendWithRetryAsync(
        long chatId,
        string text,
        int bookingId,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.None,
                    replyMarkup: TelegramKeyboards.BackToMainMenu(),
                    cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Telegram-уведомление о заявке #{BookingId} отправлено в chat {ChatId}.",
                    bookingId,
                    chatId);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Telegram-уведомление о заявке #{BookingId} не отправлено (попытка {Attempt}/{MaxAttempts}), повтор через 2 с.",
                    bookingId,
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Не удалось отправить уведомление о заявке #{BookingId} в Telegram (chat {ChatId}).",
                    bookingId,
                    chatId);
            }
        }
    }
}
