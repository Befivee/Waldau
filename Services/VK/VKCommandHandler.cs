using System.Text.Json;
using Microsoft.Extensions.Options;
using WaldauCastle.Options;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.VK;

public class VKCommandHandler(
    IServiceScopeFactory scopeFactory,
    VKApiClient apiClient,
    VKStateService stateService,
    IOptions<VKOptions> options,
    ILogger<VKCommandHandler> logger)
{
    public async Task HandleUpdateAsync(JsonElement update, CancellationToken cancellationToken)
    {
        if (!update.TryGetProperty("type", out var typeElement))
            return;

        var updateType = typeElement.GetString();

        try
        {
            switch (updateType)
            {
                case "message_new":
                    await HandleMessageNewAsync(update, cancellationToken);
                    break;
                case "message_event":
                    await HandleMessageEventAsync(update, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Необработанная ошибка VK update {UpdateType}", updateType);
        }
    }

    private async Task HandleMessageNewAsync(JsonElement update, CancellationToken cancellationToken)
    {
        if (!update.TryGetProperty("object", out var objectElement) ||
            !objectElement.TryGetProperty("message", out var messageElement))
        {
            return;
        }

        var message = messageElement.Deserialize<VkMessage>();
        if (message is null || message.Out != 0)
            return;

        var peerId = message.PeerId;
        var fromId = message.FromId;
        var text = message.Text.Trim();

        logger.LogInformation(
            "VK message_new: peer {PeerId}, from {FromId}, messageId {MessageId}",
            peerId,
            fromId,
            message.Id);

        if (!IsAdmin(fromId))
        {
            if (CastleAdminContentService.IsExcursionsRequest(text))
            {
                await WithContent(async c =>
                {
                    var body = await c.BuildExcursionsTextAsync(cancellationToken);
                    await apiClient.SendMessageAsync(
                        peerId,
                        body + $"\n\nЗапись: {SiteSettings.DefaultBaseUrl}",
                        cancellationToken);
                }, cancellationToken);
                return;
            }

            await apiClient.SendMessageAsync(
                peerId,
                CastleAdminContentService.BuildPublicWelcomeText(SiteSettings.DefaultBaseUrl),
                cancellationToken);
            return;
        }

        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "начать", StringComparison.OrdinalIgnoreCase))
        {
            stateService.GetOrCreate(peerId).Reset();
            await WithManager(m => m.SendMainMenuAsync(peerId, cancellationToken), cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);

        if (message.HasPhoto)
        {
            if (session.State is VKBotState.WaitingForEventImage or VKBotState.WaitingForNewImage
                or VKBotState.WaitingForExcursionImage or VKBotState.WaitingForNewExcursionImage)
            {
                await WithManager(m => m.HandlePhotoMessageAsync(peerId, message, cancellationToken), cancellationToken);
                return;
            }

            await apiClient.SendMessageAsync(peerId, "🖼 Сейчас фото не ожидается.", cancellationToken: cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            if (session.State != VKBotState.None)
            {
                await WithManager(m => m.HandleTextMessageAsync(peerId, text, cancellationToken), cancellationToken);
                return;
            }

            await WithManager(m => m.HandleMenuTextAsync(peerId, text, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleMessageEventAsync(JsonElement update, CancellationToken cancellationToken)
    {
        if (!update.TryGetProperty("object", out var objectElement))
            return;

        var userId = objectElement.GetProperty("user_id").GetInt64();
        var peerId = objectElement.GetProperty("peer_id").GetInt64();
        var eventId = objectElement.GetProperty("event_id").GetString() ?? string.Empty;

        if (!IsAdmin(userId))
        {
            await apiClient.SendEventAnswerAsync(eventId, userId, peerId, "⛔ Доступ запрещён.", cancellationToken);
            return;
        }

        var payload = ParsePayload(objectElement);
        logger.LogInformation(
            "VK message_event: peer {PeerId}, user {UserId}, payload {Payload}",
            peerId,
            userId,
            payload);

        await apiClient.SendEventAnswerAsync(eventId, userId, peerId, cancellationToken: cancellationToken);
        await WithManager(m => m.HandleCallbackAsync(peerId, payload, cancellationToken), cancellationToken);
    }

    private static string ParsePayload(JsonElement objectElement)
    {
        if (!objectElement.TryGetProperty("payload", out var payloadElement))
            return string.Empty;

        return ExtractCommand(payloadElement);
    }

    private static string ExtractCommand(JsonElement payloadElement)
    {
        switch (payloadElement.ValueKind)
        {
            case JsonValueKind.String:
            {
                var raw = payloadElement.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return string.Empty;

                if (raw.TrimStart().StartsWith('{'))
                {
                    using var doc = JsonDocument.Parse(raw);
                    return ExtractCommand(doc.RootElement);
                }

                return raw;
            }
            case JsonValueKind.Object:
                if (payloadElement.TryGetProperty("cmd", out var cmd))
                    return cmd.GetString() ?? string.Empty;

                return string.Empty;
            default:
                return string.Empty;
        }
    }

    private bool IsAdmin(long userId) =>
        options.Value.TryGetAdminUserId(out var adminId) && adminId == userId;

    private async Task WithManager(Func<VKAdminManager, Task> action, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<VKAdminManager>();
        await action(manager);
    }

    private async Task WithContent(Func<CastleAdminContentService, Task> action, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<CastleAdminContentService>();
        await action(content);
    }
}
