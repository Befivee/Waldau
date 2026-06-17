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

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(45),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(90)
    ];

    public async Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var telegram = options.Value;
        if (!telegram.IsConfigured)
            return;

        var adminChatIds = telegram.GetAdminChatIds();
        if (adminChatIds.Count == 0)
            return;

        var text = BookingNotificationText.Format(booking);
        var bot = CreateBotClient(telegram);

        foreach (var chatId in adminChatIds)
        {
            await SendWithPersistentRetryAsync(bot, chatId, text, booking.Id, cancellationToken);
        }
    }

    private async Task SendWithPersistentRetryAsync(
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
                await bot.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.None,
                    replyMarkup: TelegramKeyboards.BackToMainMenu(),
                    cancellationToken: cancellationToken);

                logger.LogInformation(
                    "Telegram-уведомление о заявке #{BookingId} отправлено в chat {ChatId} (попытка {Attempt}).",
                    bookingId,
                    chatId,
                    attempt);
                return;
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
