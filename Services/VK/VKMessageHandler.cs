using System.Text.Json;
using Microsoft.Extensions.Options;
using WaldauCastle.Options;

namespace WaldauCastle.Services.VK;

public class VKMessageHandler(
    VKApiClient apiClient,
    IOptions<SiteSettings> siteSettings,
    ILogger<VKMessageHandler> logger)
{
    public async Task HandleUpdateAsync(JsonElement update, CancellationToken cancellationToken)
    {
        if (!update.TryGetProperty("type", out var typeElement))
            return;

        var updateType = typeElement.GetString();
        if (!string.Equals(updateType, "message_new", StringComparison.Ordinal))
            return;

        if (!update.TryGetProperty("object", out var objectElement) ||
            !objectElement.TryGetProperty("message", out var messageElement))
        {
            return;
        }

        var message = messageElement.Deserialize<VkMessage>();
        if (message is null || message.Out != 0)
            return;

        var text = message.Text.Trim();
        logger.LogInformation(
            "VK message_new: peer {PeerId}, from {FromId}, messageId {MessageId}",
            message.PeerId,
            message.FromId,
            message.Id);

        var reply = BuildReply(text);
        await apiClient.SendMessageAsync(message.PeerId, reply, cancellationToken);

        logger.LogDebug("VK reply sent to peer {PeerId}", message.PeerId);
    }

    private string BuildReply(string incomingText)
    {
        var siteUrl = siteSettings.Value.BaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(siteUrl))
            siteUrl = SiteSettings.DefaultBaseUrl;

        if (string.Equals(incomingText, "/start", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(incomingText, "начать", StringComparison.OrdinalIgnoreCase))
        {
            return
                "Здравствуйте! Это сообщество «Замок Вальдау».\n" +
                $"Сайт: {siteUrl}\n" +
                "Напишите ваш вопрос — мы получили ваше сообщение.";
        }

        if (string.IsNullOrWhiteSpace(incomingText))
        {
            return
                "Сообщение получено. Для записи на экскурсию и афиши мероприятий посетите наш сайт:\n" +
                siteUrl;
        }

        return
            $"Спасибо за сообщение! Вы написали: «{incomingText}».\n" +
            $"Подробнее на сайте: {siteUrl}";
    }
}
