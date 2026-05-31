using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WaldauCastle.Options;

namespace WaldauCastle.Services.VK;

public class VKApiClient(
    HttpClient httpClient,
    IOptions<VKOptions> options,
    ILogger<VKApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly VKOptions _options = options.Value;

    public async Task<VkLongPollServer> GetLongPollServerAsync(CancellationToken cancellationToken)
    {
        if (!_options.TryGetGroupId(out var groupId))
            throw new InvalidOperationException("VK GroupId is not configured.");

        await EnsureLongPollSettingsAsync(groupId, cancellationToken);

        var query = new Dictionary<string, string?>
        {
            ["group_id"] = groupId.ToString(),
            ["access_token"] = _options.AccessToken,
            ["v"] = _options.ApiVersion
        };

        return await GetMethodAsync<VkLongPollServer>("groups.getLongPollServer", query, cancellationToken);
    }

    public async Task<VkLongPollCheckResult> CheckLongPollAsync(
        string server,
        string key,
        string ts,
        int waitSeconds,
        CancellationToken cancellationToken)
    {
        var uri = BuildLongPollUri(server, key, ts, waitSeconds);
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<VkLongPollCheckResult>(JsonOptions, cancellationToken);
        if (result is null)
            throw new InvalidOperationException("VK long poll returned an empty body.");

        return result;
    }

    public async Task SendMessageAsync(long peerId, string message, CancellationToken cancellationToken) =>
        await SendMessageAsync(peerId, message, keyboardJson: null, cancellationToken);

    public async Task SendMessageAsync(
        long peerId,
        string message,
        string? keyboardJson,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["peer_id"] = peerId.ToString(),
            ["message"] = message,
            ["random_id"] = Random.Shared.Next().ToString(),
            ["access_token"] = _options.AccessToken,
            ["v"] = _options.ApiVersion
        };

        if (!string.IsNullOrWhiteSpace(keyboardJson))
            query["keyboard"] = keyboardJson;

        await CallMethodAsync("messages.send", query, cancellationToken);
    }

    public async Task SendEventAnswerAsync(
        string eventId,
        long userId,
        long peerId,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["event_id"] = eventId,
            ["user_id"] = userId.ToString(),
            ["peer_id"] = peerId.ToString(),
            ["access_token"] = _options.AccessToken,
            ["v"] = _options.ApiVersion
        };

        if (!string.IsNullOrWhiteSpace(message))
            query["event_data"] = JsonSerializer.Serialize(new { type = "show_snackbar", text = message });

        await CallMethodAsync("messages.sendMessageEventAnswer", query, cancellationToken);
    }

    private async Task EnsureLongPollSettingsAsync(long groupId, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["group_id"] = groupId.ToString(),
            ["enabled"] = "1",
            ["api_version"] = _options.ApiVersion,
            ["message_new"] = "1",
            ["message_event"] = "1",
            ["access_token"] = _options.AccessToken,
            ["v"] = _options.ApiVersion
        };

        try
        {
            await CallMethodAsync("groups.setLongPollSettings", query, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось обновить Long Poll settings через API — проверьте настройки сообщества VK.");
        }
    }

    private async Task<T> GetMethodAsync<T>(
        string method,
        IReadOnlyDictionary<string, string?> query,
        CancellationToken cancellationToken)
        where T : class
    {
        var envelope = await ReadApiEnvelopeAsync<T>(method, query, cancellationToken);
        if (envelope.Response is not T payload)
            throw new InvalidOperationException($"VK API {method} returned an unexpected response.");

        return payload;
    }

    private async Task CallMethodAsync(
        string method,
        IReadOnlyDictionary<string, string?> query,
        CancellationToken cancellationToken)
    {
        _ = await ReadApiEnvelopeAsync<int>(method, query, cancellationToken);
    }

    private async Task<VkApiResponse<T>> ReadApiEnvelopeAsync<T>(
        string method,
        IReadOnlyDictionary<string, string?> query,
        CancellationToken cancellationToken)
    {
        using var body = new FormUrlEncodedContent(
            query
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => new KeyValuePair<string, string>(p.Key, p.Value!)));

        using var response = await httpClient.PostAsync(
            $"https://api.vk.com/method/{method}",
            body,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<VkApiResponse<T>>(JsonOptions, cancellationToken);
        if (envelope?.Error is { } error)
        {
            logger.LogError(
                "VK API {Method} failed: {ErrorCode} {ErrorMessage}",
                method,
                error.ErrorCode,
                error.ErrorMsg);
            throw new InvalidOperationException(
                $"VK API {method} failed ({error.ErrorCode}): {error.ErrorMsg}");
        }

        return envelope ?? throw new InvalidOperationException($"VK API {method} returned an empty response.");
    }

    private static Uri BuildLongPollUri(string server, string key, string ts, int waitSeconds)
    {
        var baseUri = server.TrimEnd('/');
        var query = string.Join('&', new[]
        {
            "act=a_check",
            $"key={Uri.EscapeDataString(key)}",
            $"ts={Uri.EscapeDataString(ts)}",
            $"wait={waitSeconds}"
        });

        return new Uri($"{baseUri}?{query}");
    }
}
