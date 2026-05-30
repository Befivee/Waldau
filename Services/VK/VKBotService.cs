using Microsoft.Extensions.Options;
using WaldauCastle.Options;

namespace WaldauCastle.Services.VK;

public class VKBotService(
    VKApiClient apiClient,
    VKMessageHandler messageHandler,
    IOptions<VKOptions> options,
    ILogger<VKBotService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.IsConfigured)
        {
            logger.LogWarning("VK-бот не настроен. Укажите VK:AccessToken и VK:GroupId в appsettings.");
            return;
        }

        var waitSeconds = Math.Clamp(options.Value.LongPollWaitSeconds, 1, 90);

        try
        {
            logger.LogInformation(
                "VK-бот запущен (Groups Long Poll, group {GroupId}, фоновый сервис).",
                options.Value.GroupId);

            var serverInfo = await apiClient.GetLongPollServerAsync(stoppingToken);
            var server = serverInfo.Server;
            var key = serverInfo.Key;
            var ts = serverInfo.Ts;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pollResult = await apiClient.CheckLongPollAsync(
                        server,
                        key,
                        ts,
                        waitSeconds,
                        stoppingToken);

                    if (pollResult.Failed is 1 or 2)
                    {
                        ts = pollResult.Ts;
                        continue;
                    }

                    if (pollResult.Failed == 3)
                    {
                        logger.LogWarning("VK long poll key expired, refreshing server credentials.");
                        serverInfo = await apiClient.GetLongPollServerAsync(stoppingToken);
                        server = serverInfo.Server;
                        key = serverInfo.Key;
                        ts = serverInfo.Ts;
                        continue;
                    }

                    ts = pollResult.Ts;

                    if (pollResult.Updates is null || pollResult.Updates.Count == 0)
                        continue;

                    foreach (var update in pollResult.Updates)
                    {
                        try
                        {
                            await messageHandler.HandleUpdateAsync(update, stoppingToken);
                        }
                        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                        {
                            logger.LogError(ex, "Ошибка обработки VK update.");
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Ошибка VK long poll. Повтор через 5 секунд.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VK-бот остановлен из-за ошибки. Веб-сервер продолжает работу.");
        }
    }
}
