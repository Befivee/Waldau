using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WaldauCastle.Options;

namespace WaldauCastle.Services.Seo;

public class IndexNowPingerService(
    IHttpClientFactory httpClientFactory,
    IOptions<SiteSettings> siteOptions,
    ILogger<IndexNowPingerService> logger) : IHostedService
{
    private static readonly string[] PingEndpoints =
    [
        "https://api.indexnow.org/indexnow",
        "https://yandex.com/indexnow"
    ];

    private static readonly string[] SitePaths =
    [
        "/",
        "/Excursion",
        "/Event",
        "/About",
        "/Directions",
        "/Contacts"
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = siteOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.IndexNowKey))
        {
            logger.LogInformation("IndexNow отключён: не задан SiteSettings:IndexNowKey.");
            return;
        }

        try
        {
            await PingAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "IndexNow: не удалось отправить ping при старте.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal async Task PingAsync(SiteSettings settings, CancellationToken cancellationToken)
    {
        var baseUrl = settings.BaseUrl.TrimEnd('/');
        var uri = new Uri(baseUrl);
        var host = GetAsciiHost(uri.Host);
        var key = settings.IndexNowKey.Trim();
        var keyLocation = $"{baseUrl}/{key}.txt";
        var urlList = SitePaths.Select(path => baseUrl + path).ToArray();

        var payload = JsonSerializer.Serialize(new
        {
            host,
            key,
            keyLocation,
            urlList
        });

        var client = httpClientFactory.CreateClient("indexnow");
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        foreach (var endpoint in PingEndpoints)
        {
            try
            {
                using var response = await client.PostAsync(endpoint, content, cancellationToken);
                logger.LogInformation(
                    "IndexNow ping {Endpoint}: {StatusCode} ({UrlCount} URL).",
                    endpoint,
                    (int)response.StatusCode,
                    urlList.Length);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "IndexNow ping не удался: {Endpoint}.", endpoint);
            }
        }
    }

    private static string GetAsciiHost(string host)
    {
        try
        {
            return new IdnMapping().GetAscii(host);
        }
        catch
        {
            return host;
        }
    }
}
