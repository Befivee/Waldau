using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WaldauCastle.Options;
using WaldauCastle.Services;

namespace WaldauCastle.Controllers;

public class EventController(
    IPublicEventCatalog events,
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramBotOptions> telegramOptions,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["MetaDescription"] =
            "Мероприятия замка Вальдау: фестивали, концерты и культурные события на территории крепости XIII века.";
        ViewData["MetaKeywords"] = "мероприятия замок Вальдау, фестивали, концерты, Низовье";
        ViewData["OgType"] = "website";

        var list = await events.GetAllAsync(cancellationToken);
        return View(list);
    }

    [HttpGet("/event-media/{fileName}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Media(string fileName, CancellationToken cancellationToken)
    {
        if (!EventMediaPath.IsSafeFileName(fileName))
            return NotFound();

        var remote = await TryFetchRemoteAsync(fileName, cancellationToken);
        if (remote is not null)
            return remote;

        var localPath = Path.Combine(environment.WebRootPath, "uploads", "events", fileName);
        if (!System.IO.File.Exists(localPath))
            return NotFound();

        return PhysicalFile(localPath, EventMediaPath.ContentType(fileName));
    }

    private async Task<IActionResult?> TryFetchRemoteAsync(string fileName, CancellationToken cancellationToken)
    {
        var telegram = telegramOptions.Value;
        var origin = telegram.RelayOrigin;
        if (string.IsNullOrWhiteSpace(origin))
            return null;

        try
        {
            var client = httpClientFactory.CreateClient("telegram_relay");
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                origin.TrimEnd('/') + "/internal/waldau/events/files/" + Uri.EscapeDataString(fileName));
            if (!string.IsNullOrWhiteSpace(telegram.RelaySecret))
                request.Headers.TryAddWithoutValidation("X-Relay-Secret", telegram.RelaySecret.Trim());

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType
                              ?? EventMediaPath.ContentType(fileName);
            return File(bytes, contentType);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
