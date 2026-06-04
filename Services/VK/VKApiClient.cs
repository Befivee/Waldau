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
    private bool _longPollSettingsApplied;

    public async Task<VkLongPollServer> GetLongPollServerAsync(CancellationToken cancellationToken) =>
        await GetLongPollServerAsync(reapplySettings: false, cancellationToken);

    public async Task<VkLongPollServer> GetLongPollServerAsync(
        bool reapplySettings,
        CancellationToken cancellationToken)
    {
        if (!_options.TryGetGroupId(out var groupId))
            throw new InvalidOperationException("VK GroupId is not configured.");

        if (reapplySettings || !_longPollSettingsApplied)
        {
            await EnsureLongPollSettingsAsync(groupId, cancellationToken);
            _longPollSettingsApplied = true;
        }

        var query = new Dictionary<string, string?>
        {
            ["group_id"] = groupId.ToString(),
            ["access_token"] = _options.AccessToken,
            ["v"] = _options.ApiVersion
        };

        using var body = new FormUrlEncodedContent(
            query
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => new KeyValuePair<string, string>(p.Key, p.Value!)));

        using var response = await httpClient.PostAsync(
            "https://api.vk.com/method/groups.getLongPollServer",
            body,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errorElement))
        {
            var code = errorElement.GetProperty("error_code").GetInt32();
            var message = errorElement.GetProperty("error_msg").GetString() ?? "unknown";
            throw new InvalidOperationException($"VK API groups.getLongPollServer failed ({code}): {message}");
        }

        var responseElement = root.GetProperty("response");
        var serverInfo = new VkLongPollServer
        {
            Key = responseElement.GetProperty("key").GetString() ?? string.Empty,
            Server = responseElement.GetProperty("server").GetString() ?? string.Empty,
            Ts = ReadJsonTs(responseElement)
        };

        logger.LogInformation("VK getLongPollServer ts={Ts}", serverInfo.Ts);

        if (string.IsNullOrWhiteSpace(serverInfo.Ts) || serverInfo.Ts == "0")
            throw new InvalidOperationException("VK getLongPollServer returned invalid ts=0. Check community Long Poll settings.");

        return serverInfo;
    }

    private static string ReadJsonTs(JsonElement root)
    {
        if (!root.TryGetProperty("ts", out var tsElement))
            return "0";

        return tsElement.ValueKind switch
        {
            JsonValueKind.String => tsElement.GetString() ?? "0",
            JsonValueKind.Number => tsElement.GetRawText(),
            _ => "0"
        };
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

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var result = new VkLongPollCheckResult
        {
            Ts = ReadJsonTs(root),
            Failed = root.TryGetProperty("failed", out var failedProp) ? failedProp.GetInt32() : null,
            Updates = root.TryGetProperty("updates", out var updatesProp)
                ? updatesProp.EnumerateArray().Select(static element => element.Clone()).ToList()
                : null
        };

        if (result.Failed is not null)
        {
            logger.LogWarning(
                "VK long poll response failed={Failed}, ts={Ts}, requestTs={RequestTs}, body={Body}",
                result.Failed,
                result.Ts,
                ts,
                body.Length > 200 ? body[..200] : body);
        }

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
