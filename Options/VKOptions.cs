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

    /// <summary>Numeric community (group) id without the minus sign.</summary>
    public long GroupId { get; set; }

    public string ApiVersion { get; set; } = "5.199";

    public int LongPollWaitSeconds { get; set; } = 25;

    public bool HasValidAccessToken =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !IsPlaceholder(AccessToken);

    public bool IsConfigured =>
        HasValidAccessToken && GroupId > 0;

    private static bool IsPlaceholder(string value) =>
        PlaceholderValues.Contains(value.Trim());
}
