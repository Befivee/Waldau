using Telegram.Bot;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.Telegram;

public partial class TelegramEventManager
{
    public async Task SendBookingsAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.None;
        session.Screen = BotScreen.Bookings;
        session.PendingDeleteBookingId = null;

        await SendBookingsPageAsync(bot, chatId, session.ListPage, cancellationToken);
    }

    private async Task SendBookingsPageAsync(
        ITelegramBotClient bot,
        long chatId,
        int page,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);

        try
        {
            var (text, totalPages) = await content.BuildBookingsPageAsync(page, cancellationToken);
            session.ListPage = Math.Clamp(page, 0, totalPages - 1);

            var list = await bookings.GetPageAsync(session.ListPage, BotListPaging.PageSize, cancellationToken);
            session.PageIds = list.Select(b => b.Id).ToList();

            await bot.SendMessage(
                chatId,
                text,
                replyMarkup: TelegramKeyboards.BookingsPage(list, session.ListPage, totalPages),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить заявки для Telegram chat {ChatId}", chatId);
            await bot.SendMessage(
                chatId,
                "⚠️ Не удалось загрузить заявки. Перезапустите бота командой /start или обратитесь к администратору сайта.",
                replyMarkup: TelegramKeyboards.BackToMainMenu(),
                cancellationToken: cancellationToken);
        }
    }

    public async Task SendBookingDeleteConfirmationAsync(
        ITelegramBotClient bot,
        long chatId,
        int bookingId,
        CancellationToken cancellationToken)
    {
        var entity = await bookings.GetByIdAsync(bookingId, cancellationToken);
        if (entity is null)
        {
            await bot.SendMessage(chatId, "Заявка не найдена.", cancellationToken: cancellationToken);
            await SendBookingsAsync(bot, chatId, cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.PendingDeleteBookingId = bookingId;

        await bot.SendMessage(
            chatId,
            content.BuildBookingDeletePrompt(entity),
            replyMarkup: TelegramKeyboards.DeleteConfirmation(),
            cancellationToken: cancellationToken);
    }

    public async Task DeleteBookingAsync(
        ITelegramBotClient bot,
        long chatId,
        int bookingId,
        CancellationToken cancellationToken)
    {
        var entity = await bookings.GetByIdAsync(bookingId, cancellationToken);
        if (entity is null)
            await bot.SendMessage(chatId, "Заявка уже удалена.", cancellationToken: cancellationToken);
        else
        {
            await bookings.DeleteAsync(bookingId, cancellationToken);
            await bot.SendMessage(chatId, "✅ Заявка удалена.", cancellationToken: cancellationToken);
        }

        var session = stateService.GetOrCreate(chatId);
        session.PendingDeleteBookingId = null;
        await SendBookingsPageAsync(bot, chatId, session.ListPage, cancellationToken);
    }
}
