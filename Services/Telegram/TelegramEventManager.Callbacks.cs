using Telegram.Bot;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.Telegram;

public partial class TelegramEventManager
{
    public async Task HandleCallbackAsync(
        ITelegramBotClient bot,
        long chatId,
        string payload,
        CancellationToken cancellationToken)
    {
        await (payload switch
        {
            BotCallbackData.MenuMain => SendMainMenuAsync(bot, chatId, cancellationToken),
            BotCallbackData.MenuBookings => SendBookingsAsync(bot, chatId, cancellationToken),
            BotCallbackData.MenuEvents => SendEventsListWithResetAsync(bot, chatId, cancellationToken),
            BotCallbackData.EventBackList => SendEventsListAsync(bot, chatId, cancellationToken),
            BotCallbackData.MenuStats => SendStatisticsAsync(bot, chatId, cancellationToken),
            BotCallbackData.PagePrev => ChangeListPageAsync(bot, chatId, -1, cancellationToken),
            BotCallbackData.PageNext => ChangeListPageAsync(bot, chatId, 1, cancellationToken),
            BotCallbackData.EventAdd => StartAddWizardAsync(bot, chatId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:view:", out var viewId) =>
                SendEventDetailsAsync(bot, chatId, viewId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:edit_title:", out var titleId) =>
                StartEditTitleAsync(bot, chatId, titleId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:edit_desc:", out var descId) =>
                StartEditDescriptionAsync(bot, chatId, descId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:edit_img:", out var imgId) =>
                StartEditImageAsync(bot, chatId, imgId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:del:", out var delId) =>
                SendDeleteConfirmationAsync(bot, chatId, delId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:del_yes:", out var delYesId) =>
                DeleteEventAsync(bot, chatId, delYesId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:del_no:", out var delNoId) =>
                SendEventDetailsAsync(bot, chatId, delNoId, cancellationToken),
            _ when BotCallbackData.TryParseBookingId(payload, "book:del:", out var bookDelId) =>
                SendBookingDeleteConfirmationAsync(bot, chatId, bookDelId, cancellationToken),
            _ when BotCallbackData.TryParseBookingId(payload, "book:del_yes:", out var bookDelYesId) =>
                DeleteBookingAsync(bot, chatId, bookDelYesId, cancellationToken),
            _ when BotCallbackData.TryParseBookingId(payload, "book:del_no:", out _) =>
                SendBookingsAsync(bot, chatId, cancellationToken),
            _ => bot.SendMessage(chatId, "Неизвестная команда. /start", cancellationToken: cancellationToken)
        });
    }

    public async Task HandleMenuTextAsync(
        ITelegramBotClient bot,
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);

        if (session.Screen is BotScreen.Excursions or BotScreen.ExcursionDetail)
        {
            session.Reset();
            await SendMainMenuAsync(bot, chatId, cancellationToken);
            return;
        }

        if (BotTextCommandResolver.TryResolveConfirmation(text, out var confirmed))
        {
            if (session.PendingDeleteBookingId is int bookingId)
            {
                if (confirmed)
                    await DeleteBookingAsync(bot, chatId, bookingId, cancellationToken);
                else
                {
                    session.PendingDeleteBookingId = null;
                    await SendBookingsAsync(bot, chatId, cancellationToken);
                }
                return;
            }

            if (session.PendingDeleteEventId is int eventId)
            {
                if (confirmed)
                    await DeleteEventAsync(bot, chatId, eventId, cancellationToken);
                else
                {
                    session.PendingDeleteEventId = null;
                    await SendEventDetailsAsync(bot, chatId, eventId, cancellationToken);
                }
                return;
            }
        }

        if (BotTextCommandResolver.TryResolve(text, session.Screen, session.PageIds, out var payload))
        {
            await HandleCallbackAsync(bot, chatId, payload, cancellationToken);
            return;
        }

        await bot.SendMessage(chatId, "Используйте /start для открытия панели управления.", cancellationToken: cancellationToken);
    }

    private async Task SendEventsListWithResetAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        stateService.GetOrCreate(chatId).ListPage = 0;
        await SendEventsListAsync(bot, chatId, cancellationToken);
    }

    private async Task ChangeListPageAsync(
        ITelegramBotClient bot,
        long chatId,
        int delta,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.ListPage = Math.Max(0, session.ListPage + delta);

        switch (session.Screen)
        {
            case BotScreen.Bookings:
                await SendBookingsPageAsync(bot, chatId, session.ListPage, cancellationToken);
                break;
            case BotScreen.Events:
                await SendEventsPageAsync(bot, chatId, session.ListPage, cancellationToken);
                break;
        }
    }
}
