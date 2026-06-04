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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunLongPollSessionAsync(groupId, waitSeconds, stoppingToken);
                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "VKBotService: ошибка сессии. Повторный запуск через 15 секунд.");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }

        logger.LogInformation("VKBotService остановлен.");
    }

    private async Task RunLongPollSessionAsync(long groupId, int waitSeconds, CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "VKBotService стартует: Groups Long Poll, group {GroupId}, api {ApiVersion}, wait {WaitSeconds}s.",
            groupId,
            options.Value.ApiVersion,
            waitSeconds);

        var serverInfo = await apiClient.GetLongPollServerAsync(reapplySettings: true, stoppingToken);

        logger.LogInformation(
            "VK Long Poll подключён: server {Server}, ts {Ts}, group {GroupId}. Ожидание событий…",
            MaskServer(serverInfo.Server),
            serverInfo.Ts,
            groupId);

        var server = serverInfo.Server;
        var key = serverInfo.Key;
        var ts = serverInfo.Ts;
        var failed2Streak = 0;

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

                if (pollResult.Failed == 1)
                {
                    if (!string.IsNullOrWhiteSpace(pollResult.Ts) && pollResult.Ts != "0")
                        ts = pollResult.Ts;

                    await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
                    continue;
                }

                if (pollResult.Failed == 2)
                {
                    failed2Streak++;
                    logger.LogWarning("VK long poll failed=2, синхронизация ts (current={Ts}, streak={Streak}).", ts, failed2Streak);
                    var resetResult = await apiClient.CheckLongPollAsync(server, key, "0", 0, stoppingToken);

                    if (resetResult.Failed == 1 && !string.IsNullOrWhiteSpace(resetResult.Ts) && resetResult.Ts != "0")
                    {
                        ts = resetResult.Ts;
                        failed2Streak = 0;
                        continue;
                    }

                    serverInfo = await apiClient.GetLongPollServerAsync(stoppingToken);
                    server = serverInfo.Server;
                    key = serverInfo.Key;

                    var delayMs = failed2Streak >= 5 ? 10_000 : 500;
                    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), stoppingToken);
                    continue;
                }

                failed2Streak = 0;

                if (pollResult.Failed == 3)
                {
                    logger.LogWarning("VK long poll key expired, refreshing server credentials.");
                    serverInfo = await apiClient.GetLongPollServerAsync(reapplySettings: true, stoppingToken);
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

                logger.LogInformation("VK long poll: получено {UpdateCount} update(s).", pollResult.Updates.Count);

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

                try
                {
                    serverInfo = await apiClient.GetLongPollServerAsync(stoppingToken);
                    server = serverInfo.Server;
                    key = serverInfo.Key;
                    ts = serverInfo.Ts;
                }
                catch (Exception refreshEx)
                {
                    logger.LogError(refreshEx, "Не удалось переподключить VK Long Poll.");
                }
            }
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
