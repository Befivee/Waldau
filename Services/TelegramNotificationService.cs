using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Models;
using WaldauCastle.Options;
using WaldauCastle.Services.Telegram;

namespace WaldauCastle.Services;

/// <summary>Отправка уведомлений через отдельный ITelegramBotClient (не блокируется long polling).</summary>
public class TelegramNotificationService(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);

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
            SendOnceAsync(chatId, text, booking.Id, cancellationToken)));

        var anySent = results.Any(success => success);
        if (!anySent)
        {
            logger.LogWarning(
                "Telegram-уведомление о заявке #{BookingId} не доставлено ни в один admin chat.",
                booking.Id);
        }

        return anySent;
    }

    private async Task<bool> SendOnceAsync(
        long chatId,
        string text,
        int bookingId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(SendTimeout);

            await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.None,
                replyMarkup: TelegramKeyboards.BackToMainMenu(),
                cancellationToken: attemptCts.Token);

            logger.LogInformation(
                "Telegram-уведомление о заявке #{BookingId} отправлено в chat {ChatId}.",
                bookingId,
                chatId);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Telegram-уведомление о заявке #{BookingId} → chat {ChatId}: таймаут {TimeoutSeconds} с (повтор через worker).",
                bookingId,
                chatId,
                SendTimeout.TotalSeconds);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Telegram-уведомление о заявке #{BookingId} не отправлено в chat {ChatId}.",
                bookingId,
                chatId);
            return false;
        }
    }
}
