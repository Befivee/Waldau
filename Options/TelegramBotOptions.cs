using System.Text.RegularExpressions;

namespace WaldauCastle.Options;

public partial class TelegramBotOptions
{
    public const string SectionName = "Telegram";

    private static readonly HashSet<string> PlaceholderValues =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "TOKEN_HERE",
            "YOUR_CHAT_ID",
            "YOUR_BOT_TOKEN",
            "CHANGEME"
        };

    public string BotToken { get; set; } = string.Empty;

    public string AdminChatId { get; set; } = string.Empty;

    public string SecondAdminChatId { get; set; } = string.Empty;

    /// <summary>SOCKS/HTTP proxy URL when Telegram is blocked from the VPS.</summary>
    public string ProxyUrl { get; set; } = string.Empty;

    /// <summary>Timeweb: do not long-poll Telegram; Hostkey owns the bot.</summary>
    public bool DisablePolling { get; set; }

    /// <summary>Timeweb: POST booking notifications here (Cloudflare tunnel to Hostkey).</summary>
    public string RelayUrl { get; set; } = string.Empty;

    /// <summary>Shared secret for Timeweb → Hostkey relay.</summary>
    public string RelaySecret { get; set; } = string.Empty;

    /// <summary>Hostkey: accept POST /internal/telegram/booking from the site.</summary>
    public bool AcceptRelay { get; set; }

    /// <summary>Hostkey: Telegram/VK + relay API only, no public website.</summary>
    public bool BotOnly { get; set; }

    public bool TryGetProxyUri(out Uri? proxyUri)
    {
        proxyUri = null;
        var raw = ProxyUrl?.Trim();
        if (string.IsNullOrWhiteSpace(raw) || IsPlaceholder(raw))
            return false;

        return Uri.TryCreate(raw, UriKind.Absolute, out proxyUri);
    }

    public bool HasValidBotToken =>
        !string.IsNullOrWhiteSpace(BotToken) &&
        !IsPlaceholder(BotToken) &&
        BotTokenRegex().IsMatch(BotToken);

    public bool IsConfigured =>
        HasValidBotToken && GetAdminChatIds().Count > 0;

    public bool TryGetAdminChatId(out long chatId)
    {
        chatId = 0;
        var ids = GetAdminChatIds();
        if (ids.Count == 0)
            return false;

        chatId = ids[0];
        return true;
    }

    public IReadOnlyList<long> GetAdminChatIds()
    {
        var ids = new List<long>();
        TryAddChatId(AdminChatId, ids);
        TryAddChatId(SecondAdminChatId, ids);
        return ids;
    }

    public bool IsAdminChat(long chatId) => GetAdminChatIds().Contains(chatId);

    private static void TryAddChatId(string raw, ICollection<long> ids)
    {
        if (string.IsNullOrWhiteSpace(raw) || IsPlaceholder(raw))
            return;

        if (long.TryParse(raw.Trim(), out var chatId) && !ids.Contains(chatId))
            ids.Add(chatId);
    }

    private static bool IsPlaceholder(string value) =>
        PlaceholderValues.Contains(value.Trim());

    [GeneratedRegex(@"^\d+:[A-Za-z0-9_-]+$")]
    private static partial Regex BotTokenRegex();
}
