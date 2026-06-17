using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Models;
using WaldauCastle.Options;
using WaldauCastle.Services.Telegram;

namespace WaldauCastle.Services;

public class TelegramNotificationService(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
    public const string HttpClientName = "telegram_notifications";

    private static readonly TimeSpan PerAttemptTimeout = TimeSpan.FromSeconds(35);

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(20)
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
        var bot = CreateBotClient(telegram);

        var results = await Task.WhenAll(adminChatIds.Select(chatId =>
            SendWithPersistentRetryAsync(bot, chatId, text, booking.Id, cancellationToken)));

        return results.All(success => success);
    }

    private async Task<bool> SendWithPersistentRetryAsync(
        ITelegramBotClient bot,
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

                await bot.SendMessage(
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
                    "Telegram-уведомление о заявке #{BookingId} не отправлено (попытка {Attempt}/{MaxAttempts}), повтор через {DelaySeconds} с.",
                    bookingId,
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Telegram-уведомление о заявке #{BookingId} не удалось отправить в chat {ChatId} после {MaxAttempts} попыток.",
                    bookingId,
                    chatId,
                    maxAttempts);
            }
        }

        return false;
    }

    private ITelegramBotClient CreateBotClient(TelegramBotOptions telegram)
    {
        var token = telegram.BotToken.Trim();
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        var clientOptions = telegram.HasApiBaseUrl
            ? new TelegramBotClientOptions(token, telegram.ApiBaseUrl.Trim()) { RetryCount = 0 }
            : new TelegramBotClientOptions(token) { RetryCount = 0 };
        return new TelegramBotClient(clientOptions, httpClient);
    }
}
