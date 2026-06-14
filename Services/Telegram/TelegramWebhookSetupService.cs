using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WaldauCastle.Options;

namespace WaldauCastle.Services.Telegram;

public class TelegramWebhookSetupService(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> telegramOptions,
    IOptions<SiteSettings> siteOptions,
    ILogger<TelegramWebhookSetupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = telegramOptions.Value;
        if (!options.IsConfigured || !options.UseWebhook)
            return;

        if (!options.HasWebhookSecret)
        {
            logger.LogWarning(
                "Telegram UseWebhook=true, но WebhookSecret не задан. Укажите Telegram:WebhookSecret в конфиге.");
            return;
        }

        var webhookUrl = siteOptions.Value.BaseUrl.TrimEnd('/') + "/api/telegram/webhook";

        try
        {
            await botClient.SetWebhook(
                url: webhookUrl,
                secretToken: options.WebhookSecret.Trim(),
                allowedUpdates: [UpdateType.Message, UpdateType.CallbackQuery],
                dropPendingUpdates: true,
                cancellationToken: cancellationToken);

            logger.LogInformation("Telegram webhook зарегистрирован: {WebhookUrl}", webhookUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось установить Telegram webhook на {WebhookUrl}", webhookUrl);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
