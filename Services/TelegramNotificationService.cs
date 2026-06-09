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

        var text = BookingNotificationText.Format(booking);

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
