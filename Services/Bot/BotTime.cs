using System.Globalization;

namespace WaldauCastle.Services.Bot;

public static class BotTime
{
    private static readonly CultureInfo RuCulture = new("ru-RU");
    private static readonly TimeZoneInfo KaliningradZone = ResolveKaliningradZone();

    public static DateTime LocalToday =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KaliningradZone).Date;

    public static string FormatLocalDateTime(DateTime dateTime)
    {
        var utc = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, KaliningradZone);
        return local.ToString("d MMMM yyyy, HH:mm", RuCulture) + " (Калининград)";
    }

    private static TimeZoneInfo ResolveKaliningradZone()
    {
        foreach (var id in new[] { "Europe/Kaliningrad", "Kaliningrad Standard Time" })
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

        return TimeZoneInfo.CreateCustomTimeZone("Kaliningrad", TimeSpan.FromHours(2), "Kaliningrad", "Kaliningrad");
    }
}
