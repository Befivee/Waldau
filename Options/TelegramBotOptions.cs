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

    public bool HasValidBotToken =>
        !string.IsNullOrWhiteSpace(BotToken) &&
        !IsPlaceholder(BotToken) &&
        BotTokenRegex().IsMatch(BotToken);

    public bool IsConfigured =>
        HasValidBotToken && TryGetAdminChatId(out _);

    public bool TryGetAdminChatId(out long chatId)
    {
        chatId = 0;
        if (string.IsNullOrWhiteSpace(AdminChatId) || IsPlaceholder(AdminChatId))
            return false;

        return long.TryParse(AdminChatId, out chatId);
    }

    private static bool IsPlaceholder(string value) =>
        PlaceholderValues.Contains(value.Trim());

    [GeneratedRegex(@"^\d+:[A-Za-z0-9_-]+$")]
    private static partial Regex BotTokenRegex();
}
