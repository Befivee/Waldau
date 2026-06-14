using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using WaldauCastle.Options;
using WaldauCastle.Services.Telegram;

namespace WaldauCastle.Controllers;

[ApiController]
[Route("api/telegram/webhook")]
public class TelegramWebhookController(
    TelegramCommandHandler commandHandler,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] Update? update,
        ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        if (!options.Value.UseWebhook)
            return NotFound();

        if (!IsAuthorized())
            return Unauthorized();

        if (update is null)
            return Ok();

        try
        {
            await commandHandler.HandleUpdateAsync(botClient, update, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обработки Telegram webhook update {UpdateId}", update.Id);
        }

        return Ok();
    }

    private bool IsAuthorized()
    {
        if (!options.Value.HasWebhookSecret)
            return true;

        var header = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
        return string.Equals(header, options.Value.WebhookSecret.Trim(), StringComparison.Ordinal);
    }
}
