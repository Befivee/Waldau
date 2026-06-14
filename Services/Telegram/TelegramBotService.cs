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

        if (options.Value.UseWebhook)
        {
            logger.LogInformation("Telegram webhook mode — long polling отключён.");
            return;
        }

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
                    var me = await botClient.GetMe(stoppingToken);
                    logger.LogInformation(
                        "Telegram CMS-бот @{BotUsername} запущен (IPv4 first: {PreferIpv4}, proxy: {Proxy}).",
                        me.Username,
                        options.Value.PreferIpv4,
                        options.Value.HasProxy ? "yes" : "no");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "GetMe не удался — polling всё равно будет запущен.");
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
