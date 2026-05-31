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

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                try
                {
                    var me = await botClient.GetMe(stoppingToken);
                    logger.LogInformation("Telegram CMS-бот @{BotUsername} запущен (фоновый сервис).", me.Username);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "GetMe не удался — polling всё равно будет запущен.");
                }

                botClient.StartReceiving(
                    updateHandler: commandHandler.HandleUpdateAsync,
                    errorHandler: commandHandler.HandleErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telegram-бот остановлен из-за ошибки. Повтор через 15 секунд.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        logger.LogInformation("TelegramBotService остановлен.");
    }
}
