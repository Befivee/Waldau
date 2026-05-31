using Microsoft.Extensions.Options;
using WaldauCastle.Options;

namespace WaldauCastle.Services.VK;

public class VKBotService(
    VKApiClient apiClient,
    VKCommandHandler commandHandler,
    IOptions<VKOptions> options,
    ILogger<VKBotService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var validation = options.Value.Validate();
        if (!validation.IsValid)
        {
            logger.LogWarning(
                "VKBotService не запущен: конфигурация не прошла проверку. {ValidationErrors}",
                validation.Summary);
            return;
        }

        options.Value.TryGetGroupId(out var groupId);
        var waitSeconds = Math.Clamp(options.Value.LongPollWaitSeconds, 1, 90);

        try
        {
            logger.LogInformation(
                "VKBotService стартует: Groups Long Poll, group {GroupId}, api {ApiVersion}, wait {WaitSeconds}s.",
                groupId,
                options.Value.ApiVersion,
                waitSeconds);

            var serverInfo = await apiClient.GetLongPollServerAsync(stoppingToken);

            logger.LogInformation(
                "VK Long Poll подключён: server {Server}, ts {Ts}, group {GroupId}. Ожидание событий…",
                MaskServer(serverInfo.Server),
                serverInfo.Ts,
                groupId);

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
                        logger.LogInformation(
                            "VK Long Poll переподключён: server {Server}, ts {Ts}.",
                            MaskServer(serverInfo.Server),
                            serverInfo.Ts);
                        continue;
                    }

                    ts = pollResult.Ts;

                    if (pollResult.Updates is null || pollResult.Updates.Count == 0)
                        continue;

                    logger.LogDebug("VK long poll: получено {UpdateCount} update(s).", pollResult.Updates.Count);

                    foreach (var update in pollResult.Updates)
                    {
                        try
                        {
                            await commandHandler.HandleUpdateAsync(update, stoppingToken);
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

            logger.LogInformation("VKBotService остановлен (cancellation requested).");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("VKBotService остановлен (graceful shutdown).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VKBotService остановлен из-за ошибки. Веб-сервер продолжает работу.");
        }
    }

    private static string MaskServer(string server)
    {
        if (string.IsNullOrWhiteSpace(server))
            return "(empty)";

        if (!Uri.TryCreate(server, UriKind.Absolute, out var uri))
            return server;

        return uri.Host;
    }
}
