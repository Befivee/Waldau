using WaldauCastle.Models;

namespace WaldauCastle.Options;

public class SiteSettings
{
    public const string SectionName = "SiteSettings";

    public const string DefaultBaseUrl = "https://вальдау.рф";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public string SiteName { get; set; } = SiteInfo.BrowserTitle;

    public string DefaultKeywords { get; set; } =
        "замок Вальдау, музей, экскурсии, Калининград, Низовье, средневековый замок";
}
