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
