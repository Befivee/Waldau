using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaldauCastle.Services.VK;

internal sealed class VkApiResponse<T>
{
    [JsonPropertyName("response")]
    public T? Response { get; set; }

    [JsonPropertyName("error")]
    public VkApiError? Error { get; set; }
}

internal sealed class VkApiError
{
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("error_msg")]
    public string ErrorMsg { get; set; } = string.Empty;
}

public sealed class VkLongPollServer
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("server")]
    public string Server { get; set; } = string.Empty;

    [JsonPropertyName("ts")]
    public string Ts { get; set; } = "0";
}

public sealed class VkLongPollCheckResult
{
    [JsonPropertyName("ts")]
    public string Ts { get; set; } = "0";

    [JsonPropertyName("updates")]
    public List<JsonElement>? Updates { get; set; }

    [JsonPropertyName("failed")]
    public int? Failed { get; set; }
}

internal sealed class VkMessage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("peer_id")]
    public long PeerId { get; set; }

    [JsonPropertyName("from_id")]
    public long FromId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("out")]
    public int Out { get; set; }
}
