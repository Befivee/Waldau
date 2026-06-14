using System.Globalization;

namespace WaldauCastle.Services.Bot;

public static class BotTime
{
    private static readonly CultureInfo RuCulture = new("ru-RU");
    private static readonly TimeZoneInfo MoscowZone = ResolveMoscowZone();

    public static DateTime MoscowToday =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowZone).Date;

    public static string FormatMoscowDateTime(DateTime dateTime)
    {
        var utc = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        var moscow = TimeZoneInfo.ConvertTimeFromUtc(utc, MoscowZone);
        return moscow.ToString("d MMMM yyyy, HH:mm", RuCulture) + " (МСК)";
    }

    private static TimeZoneInfo ResolveMoscowZone()
    {
        foreach (var id in new[] { "Europe/Moscow", "Russian Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("MSK", TimeSpan.FromHours(3), "MSK", "MSK");
    }
}
