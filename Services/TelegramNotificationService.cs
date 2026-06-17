using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Models;
using WaldauCastle.Options;
using WaldauCastle.Services.Telegram;

namespace WaldauCastle.Services;

/// <summary>Отправка уведомлений через тот же ITelegramBotClient, что и CMS-бот (проверенное соединение).</summary>
public class TelegramNotificationService(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    private static readonly TimeSpan PerAttemptTimeout = TimeSpan.FromSeconds(20);

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(3)
    ];

    public async Task<bool> NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var telegram = options.Value;
        if (!telegram.IsConfigured)
            return true;

        var adminChatIds = telegram.GetAdminChatIds();
        if (adminChatIds.Count == 0)
            return true;

        var text = BookingNotificationText.Format(booking);
        logger.LogInformation(
            "Отправка Telegram-уведомления о заявке #{BookingId} в {Count} chat(s).",
            booking.Id,
            adminChatIds.Count);

        var results = await Task.WhenAll(adminChatIds.Select(chatId =>
            SendWithRetryAsync(chatId, text, booking.Id, cancellationToken)));

        var anySent = results.Any(success => success);
        if (!anySent)
        {
            logger.LogWarning(
                "Telegram-уведомление о заявке #{BookingId} не доставлено ни в один admin chat.",
                booking.Id);
        }

        return anySent;
    }

    private async Task<bool> SendWithRetryAsync(
        long chatId,
        string text,
        int bookingId,
        CancellationToken cancellationToken)
    {
        var maxAttempts = RetryDelays.Length + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                attemptCts.CancelAfter(PerAttemptTimeout);

                await botClient.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.None,
                    replyMarkup: TelegramKeyboards.BackToMainMenu(),
                    cancellationToken: attemptCts.Token);

                logger.LogInformation(
                    "Telegram-уведомление о заявке #{BookingId} отправлено в chat {ChatId} (попытка {Attempt}).",
                    bookingId,
                    chatId,
                    attempt);
                return true;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = RetryDelays[attempt - 1];
                logger.LogWarning(
                    ex,
                    "Telegram-уведомление о заявке #{BookingId} → chat {ChatId}: попытка {Attempt}/{MaxAttempts}, повтор через {DelaySeconds} с.",
                    bookingId,
                    chatId,
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Telegram-уведомление о заявке #{BookingId} не отправлено в chat {ChatId} после {MaxAttempts} попыток.",
                    bookingId,
                    chatId,
                    maxAttempts);
            }
        }

        return false;
    }
}
