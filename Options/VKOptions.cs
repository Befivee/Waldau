using Microsoft.Extensions.Configuration;

namespace WaldauCastle.Options;

public class VKOptions
{
    public const string SectionName = "VK";

    private static readonly HashSet<string> PlaceholderValues =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "TOKEN_HERE",
            "YOUR_VK_TOKEN",
            "CHANGEME",
            "ACCESS_TOKEN_HERE"
        };

    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Community id: numeric (<c>123456789</c>), with prefix (<c>club123456789</c>) or negative VK id (<c>-123456789</c>).
    /// Stored as string so environment variables bind reliably without silent long parse failures.
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "5.199";

    public int LongPollWaitSeconds { get; set; } = 25;

    public bool HasValidAccessToken =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !IsPlaceholder(AccessToken);

    public bool IsConfigured => Validate().IsValid;

    public VKOptionsValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(AccessToken))
            errors.Add("VK:AccessToken (или VK__AccessToken) не задан.");
        else if (IsPlaceholder(AccessToken))
            errors.Add("VK:AccessToken содержит placeholder — замените на реальный токен сообщества.");
        else if (AccessToken.Trim().Length < 10)
            errors.Add("VK:AccessToken слишком короткий — проверьте значение токена.");

        if (string.IsNullOrWhiteSpace(GroupId))
            errors.Add("VK:GroupId (или VK__GroupId) не задан.");
        else if (!TryGetGroupId(out var groupId))
            errors.Add($"VK:GroupId имеет неверный формат: «{GroupId.Trim()}». Ожидается число или club123456789.");
        else if (groupId <= 0)
            errors.Add($"VK:GroupId должен быть положительным числом, получено: {groupId}.");

        if (string.IsNullOrWhiteSpace(ApiVersion))
            errors.Add("VK:ApiVersion (или VK__ApiVersion) не задан.");
        else if (!ApiVersion.Trim().StartsWith("5.", StringComparison.Ordinal))
            errors.Add($"VK:ApiVersion выглядит некорректно: «{ApiVersion.Trim()}». Ожидается формат 5.xxx.");

        if (LongPollWaitSeconds is < 1 or > 90)
        {
            errors.Add(
                $"VK:LongPollWaitSeconds={LongPollWaitSeconds} вне диапазона 1–90 (будет принудительно ограничено при работе).");
        }

        return new VKOptionsValidationResult(errors);
    }

    /// <summary>Returns positive numeric community id for VK API calls.</summary>
    public bool TryGetGroupId(out long groupId)
    {
        groupId = 0;
        if (string.IsNullOrWhiteSpace(GroupId))
            return false;

        var raw = GroupId.Trim();

        if (raw.StartsWith('-'))
            raw = raw[1..];

        if (raw.StartsWith("club", StringComparison.OrdinalIgnoreCase))
            raw = raw[4..];
        else if (raw.StartsWith("public", StringComparison.OrdinalIgnoreCase))
            raw = raw[6..];

        if (!long.TryParse(raw, out groupId))
            return false;

        groupId = Math.Abs(groupId);
        return groupId > 0;
    }

    public static VKOptions Load(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var options = new VKOptions();

        try
        {
            section.Bind(options);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or ArgumentException)
        {
            // Invalid env value (e.g. VK__LongPollWaitSeconds=abc) — keep defaults; Validate() reports details.
        }

        return options;
    }

    private static bool IsPlaceholder(string value) =>
        PlaceholderValues.Contains(value.Trim());
}

public sealed class VKOptionsValidationResult
{
    public VKOptionsValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public string Summary => IsValid
        ? "OK"
        : string.Join("; ", Errors);
}
