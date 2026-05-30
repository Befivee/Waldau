using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Options;

namespace WaldauCastle.Services.Telegram;

public class TelegramBotService(
    ITelegramBotClient botClient,
    TelegramCommandHandler commandHandler,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramBotService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.IsConfigured)
        {
            logger.LogWarning("Telegram-бот не настроен. Укажите BotToken и AdminChatId в appsettings.");
            return;
        }

        try
        {
            var me = await botClient.GetMe(stoppingToken);
            logger.LogInformation("Telegram CMS-бот @{BotUsername} запущен (фоновый сервис).", me.Username);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
            };

            botClient.StartReceiving(
                updateHandler: commandHandler.HandleUpdateAsync,
                errorHandler: commandHandler.HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            // Do not crash the web host if Telegram API is unavailable.
            logger.LogError(ex, "Telegram-бот остановлен из-за ошибки. Веб-сервер продолжает работу.");
        }
    }
}
