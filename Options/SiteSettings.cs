using WaldauCastle.Models;

namespace WaldauCastle.Options;

public class SiteSettings
{
    public const string SectionName = "SiteSettings";

    public const string DefaultBaseUrl = "https://вальдау.рф";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public string SiteName { get; set; } = SiteInfo.BrowserTitle;

    /// <summary>Ключ IndexNow (файл /{ключ}.txt на сайте). Пусто — ping отключён.</summary>
    public string IndexNowKey { get; set; } = "waldau8f3c2a1b5e9d4";

    public string DefaultKeywords { get; set; } =
        "замок Вальдау, вальдау сайт, замок Вальдау официальный сайт, Вальдау, Waldau, музей, экскурсии, Калининград, Низовье, средневековый замок";
}
