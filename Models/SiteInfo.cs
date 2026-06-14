using System.Globalization;

namespace WaldauCastle.Models;

public static class SiteInfo
{
    public const string CastleName = "Замок Вальдау";
    public const string HomePageTitle = "Замок Вальдау - Официальный сайт";
    public const string BrowserTitle = HomePageTitle;
    public const string LogoPath = "/images/logo.svg";
    public const string LogoPngPath = "/images/logo.png";
    public const string FaviconPath = "/favicon.ico";
    public const string FaviconPngPath = "/images/favicon-256.png";
    public const string Tagline = "Средневековая крепость XIII века";
    public const string Location = "пос. Низовье, Калининградская область";
    public const string Address = "238530, Калининградская обл., пос. Низовье, ул. Замковая, 1";
    public const string Phone = "+7 (963) 299-85-43";
    public const string PhoneTel = "+79632998543";
    public const string Phone2 = "+7 (995) 672-48-01";
    public const string Phone2Tel = "+79956724801";
    public const string VkUrl = "https://vk.com/waldau1264";
    public const string VkLabel = "ВКонтакте vk.com/waldau1264";
    public const string TelegramUrl = "https://t.me/castleWaldau";
    public const string TelegramLabel = "Telegram t.me/castleWaldau";
    public const string WorkingHours = "10:00 — 18:00, без выходных";
    public const string TicketPrice = "от 650 ₽";
    public const string BusRoute = "Автобус №110";
    public const string DistanceFromKg = "15 км от центра Калининграда";
    public const string TravelTime = "~45 мин на автобусе";
    public const double Latitude = 54.700653;
    public const double Longitude = 20.743124;

    public static string GetDocumentTitle(string? pageTitle, string? metaTitle) =>
        !string.IsNullOrWhiteSpace(metaTitle)
            ? metaTitle.Trim()
            : string.IsNullOrWhiteSpace(pageTitle) || pageTitle == "Главная"
                ? HomePageTitle
                : $"{pageTitle.Trim()} — {HomePageTitle}";

    public static string GetBrowserTitle(string? pageTitle) =>
        GetDocumentTitle(pageTitle, null);

    public static string YandexMapsUrl =>
        $"https://yandex.ru/maps/?pt={Longitude.ToString(CultureInfo.InvariantCulture)},{Latitude.ToString(CultureInfo.InvariantCulture)}&z=15&l=map";

    public static string YandexMapEmbedUrl =>
        $"https://yandex.ru/map-widget/v1/?ll={Longitude.ToString(CultureInfo.InvariantCulture)}%2C{Latitude.ToString(CultureInfo.InvariantCulture)}&z=15&l=map&pt={Longitude.ToString(CultureInfo.InvariantCulture)}%2C{Latitude.ToString(CultureInfo.InvariantCulture)}";
}