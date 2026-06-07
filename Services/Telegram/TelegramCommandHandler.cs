using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using WaldauCastle.Options;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.Telegram;

public class TelegramCommandHandler(
    IServiceScopeFactory scopeFactory,
    TelegramStateService stateService,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramCommandHandler> logger) : IUpdateHandler
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

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        if (IsPollingTimeout(exception))
        {
            logger.LogDebug(exception, "Telegram long poll timeout ({Source}).", source);
            return Task.CompletedTask;
        }

        logger.LogError(exception, "Ошибка Telegram polling ({Source}).", source);
        return Task.CompletedTask;
    }

    private static bool IsPollingTimeout(Exception exception) =>
        exception is global::Telegram.Bot.Exceptions.RequestException
        {
            InnerException: TaskCanceledException or TimeoutException
        };

    private async Task HandleMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;

        if (!IsAdmin(chatId))
        {
            if (IsStartCommand(message.Text))
            {
                await botClient.SendMessage(
                    chatId,
                    CastleAdminContentService.BuildPublicWelcomeText(GetSiteUrl()),
                    cancellationToken: cancellationToken);
                return;
            }

            if (CastleAdminContentService.IsExcursionsRequest(message.Text))
            {
                await WithContent(c => SendPublicExcursionsAsync(botClient, chatId, c, cancellationToken), cancellationToken);
                return;
            }

            await botClient.SendMessage(
                chatId,
                CastleAdminContentService.BuildPublicWelcomeText(GetSiteUrl()),
                cancellationToken: cancellationToken);
            return;
        }

        if (message.Text?.StartsWith("/start", StringComparison.OrdinalIgnoreCase) == true)
        {
            stateService.GetOrCreate(chatId).Reset();
            await WithManager(m => m.SendMainMenuAsync(botClient, chatId, cancellationToken), cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        NormalizeLegacySession(session);

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

            await WithManager(m => m.HandleMenuTextAsync(botClient, chatId, message.Text.Trim(), cancellationToken), cancellationToken);
            return;
        }

        await botClient.SendMessage(
            chatId,
            "Команда не распознана. Отправьте /start для открытия панели управления.",
            cancellationToken: cancellationToken);
    }

    private static void NormalizeLegacySession(TelegramUserSession session)
    {
        if (session.Screen is BotScreen.Excursions or BotScreen.ExcursionDetail)
            session.Reset();

        if ((int)session.State > (int)TelegramBotState.WaitingForNewImage)
        {
            session.State = TelegramBotState.None;
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
        await WithManager(m => m.HandleCallbackAsync(botClient, chatId.Value, data, cancellationToken), cancellationToken);
    }

    private static bool IsStartCommand(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var command = text.Trim().Split([' ', '@'])[0];
        return command.Equals("/start", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAdmin(long chatId) => options.Value.IsAdminChat(chatId);

    private async Task WithContent(Func<CastleAdminContentService, Task> action, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<CastleAdminContentService>();
        await action(content);
    }

    private async Task SendPublicExcursionsAsync(
        ITelegramBotClient botClient,
        long chatId,
        CastleAdminContentService content,
        CancellationToken cancellationToken)
    {
        var text = await content.BuildExcursionsTextAsync(cancellationToken);
        await botClient.SendMessage(
            chatId,
            text + $"\n\nЗапись: {GetSiteUrl()}",
            cancellationToken: cancellationToken);
    }

    private string GetSiteUrl() => SiteSettings.DefaultBaseUrl;

    private async Task WithManager(Func<TelegramEventManager, Task> action, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<TelegramEventManager>();
        await action(manager);
    }
}
