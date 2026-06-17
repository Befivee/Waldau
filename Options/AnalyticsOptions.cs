namespace WaldauCastle.Options;

public class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public string GoogleMeasurementId { get; set; } = "";

    public string YandexMetrikaId { get; set; } = "";

    public bool HasGoogle => !string.IsNullOrWhiteSpace(GoogleMeasurementId);

    public bool HasYandex => !string.IsNullOrWhiteSpace(YandexMetrikaId);

    public bool HasAny => HasGoogle || HasYandex;
}
