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
        var telegram = options.Value;
        if (!telegram.IsConfigured)
        {
            logger.LogWarning("Telegram-бот не настроен. Укажите BotToken и AdminChatId.");
            return;
        }

        if (!telegram.HasProxy)
            logger.LogWarning(
                "Telegram ProxyUrl не задан — на VPS в РФ бот может не отвечать. Укажите Telegram__ProxyUrl.");

        var dropPendingUpdates = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
                DropPendingUpdates = dropPendingUpdates
            };
            dropPendingUpdates = false;

            try
            {
                try
                {
                    await botClient.DeleteWebhook(dropPendingUpdates: false, cancellationToken: stoppingToken);
                    var me = await botClient.GetMe(stoppingToken);
                    logger.LogInformation(
                        "Telegram CMS-бот @{BotUsername} запущен (long polling, proxy: {Proxy}).",
                        me.Username,
                        telegram.HasProxy ? "задан" : "нет");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "GetMe/DeleteWebhook не удался — polling всё равно будет запущен.");
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                await botClient.ReceiveAsync(
                    updateHandler: commandHandler,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telegram polling прерван. Повтор через 15 секунд.");
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
