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
            try
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: text,
                    parseMode: ParseMode.None,
                    replyMarkup: TelegramKeyboards.BackToMainMenu(),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось отправить уведомление о заявке в Telegram (chat {ChatId}).", chatId);
            }
        }
    }

    private ITelegramBotClient CreateBotClient(TelegramBotOptions telegram)
    {
        var token = telegram.BotToken.Trim();
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        var clientOptions = telegram.HasApiBaseUrl
            ? new TelegramBotClientOptions(token, telegram.ApiBaseUrl.Trim()) { RetryCount = 1 }
            : new TelegramBotClientOptions(token) { RetryCount = 1 };
        return new TelegramBotClient(clientOptions, httpClient);
    }
}
