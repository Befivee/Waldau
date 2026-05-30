namespace WaldauCastle.Options;

public class SiteSettings
{
    public const string SectionName = "SiteSettings";

    public string BaseUrl { get; set; } = "https://waldau-castle.ru";

    public string SiteName { get; set; } = "Замок Вальдау";

    public string DefaultKeywords { get; set; } =
        "замок Вальдау, музей, экскурсии, Калининград, Низовье, средневековый замок";
}
