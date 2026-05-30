using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Options;

namespace WaldauCastle.Services.Telegram;

public class TelegramCommandHandler(
    IServiceScopeFactory scopeFactory,
    TelegramStateService stateService,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramCommandHandler> logger)
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.CallbackQuery is { } callback)
            {
                await HandleCallbackQueryAsync(botClient, callback, cancellationToken);
                return;
            }

            if (update.Message is { } message)
                await HandleMessageAsync(botClient, message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Необработанная ошибка Telegram update {UpdateId}", update.Id);
            var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
            if (chatId.HasValue)
            {
                await botClient.SendMessage(
                    chatId.Value,
                    "⚠️ Произошла ошибка. Попробуйте /start.",
                    cancellationToken: cancellationToken);
            }
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Ошибка Telegram polling.");
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!IsAdmin(chatId))
        {
            await botClient.SendMessage(chatId, "⛔ Доступ запрещён.", cancellationToken: cancellationToken);
            return;
        }

        if (message.Text?.StartsWith("/start", StringComparison.OrdinalIgnoreCase) == true)
        {
            stateService.GetOrCreate(chatId).Reset();
            await WithManager(m => m.SendMainMenuAsync(botClient, chatId, cancellationToken), cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);

        if (message.Photo is { Length: > 0 })
        {
            if (session.State is TelegramBotState.WaitingForEventImage or TelegramBotState.WaitingForNewImage)
            {
                await WithManager(m => m.HandlePhotoMessageAsync(botClient, message, cancellationToken), cancellationToken);
                return;
            }

            await botClient.SendMessage(chatId, "🖼 Сейчас фото не ожидается.", cancellationToken: cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            if (session.State != TelegramBotState.None)
            {
                await WithManager(m => m.HandleTextMessageAsync(botClient, message, cancellationToken), cancellationToken);
                return;
            }

            await botClient.SendMessage(
                chatId,
                "Используйте /start для открытия панели управления.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callback,
        CancellationToken cancellationToken)
    {
        var chatId = callback.Message?.Chat.Id;
        if (chatId is null)
            return;

        await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

        if (!IsAdmin(chatId.Value))
        {
            await botClient.SendMessage(chatId.Value, "⛔ Доступ запрещён.", cancellationToken: cancellationToken);
            return;
        }

        var data = callback.Data ?? string.Empty;

        await (data switch
        {
            TelegramCallbackData.MenuMain =>
                WithManager(m => m.SendMainMenuAsync(botClient, chatId.Value, cancellationToken), cancellationToken),

            TelegramCallbackData.MenuBookings =>
                WithManager(m => m.SendBookingsAsync(botClient, chatId.Value, cancellationToken), cancellationToken),

            TelegramCallbackData.MenuEvents or TelegramCallbackData.EventBackList =>
                WithManager(m => m.SendEventsListAsync(botClient, chatId.Value, cancellationToken), cancellationToken),

            TelegramCallbackData.MenuStats =>
                WithManager(m => m.SendStatisticsAsync(botClient, chatId.Value, cancellationToken), cancellationToken),

            TelegramCallbackData.EventAdd =>
                WithManager(m => m.StartAddWizardAsync(botClient, chatId.Value, cancellationToken), cancellationToken),

            _ when TelegramCallbackData.TryParseEventId(data, "evt:view:", out var viewId) =>
                WithManager(m => m.SendEventDetailsAsync(botClient, chatId.Value, viewId, cancellationToken), cancellationToken),

            _ when TelegramCallbackData.TryParseEventId(data, "evt:edit_title:", out var titleId) =>
                WithManager(m => m.StartEditTitleAsync(botClient, chatId.Value, titleId, cancellationToken), cancellationToken),

            _ when TelegramCallbackData.TryParseEventId(data, "evt:edit_desc:", out var descId) =>
                WithManager(m => m.StartEditDescriptionAsync(botClient, chatId.Value, descId, cancellationToken), cancellationToken),

            _ when TelegramCallbackData.TryParseEventId(data, "evt:edit_img:", out var imgId) =>
                WithManager(m => m.StartEditImageAsync(botClient, chatId.Value, imgId, cancellationToken), cancellationToken),

            _ when TelegramCallbackData.TryParseEventId(data, "evt:del:", out var delId) =>
                WithManager(m => m.SendDeleteConfirmationAsync(botClient, chatId.Value, delId, cancellationToken), cancellationToken),

            _ when TelegramCallbackData.TryParseEventId(data, "evt:del_yes:", out var delYesId) =>
                WithManager(m => m.DeleteEventAsync(botClient, chatId.Value, delYesId, cancellationToken), cancellationToken),

            _ when TelegramCallbackData.TryParseEventId(data, "evt:del_no:", out var delNoId) =>
                WithManager(m => m.SendEventDetailsAsync(botClient, chatId.Value, delNoId, cancellationToken), cancellationToken),

            _ => botClient.SendMessage(chatId.Value, "Неизвестная команда. /start", cancellationToken: cancellationToken)
        });
    }

    private bool IsAdmin(long chatId) =>
        options.Value.TryGetAdminChatId(out var adminId) && adminId == chatId;

    private async Task WithManager(Func<TelegramEventManager, Task> action, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<TelegramEventManager>();
        await action(manager);
    }
}
