namespace WaldauCastle.Services.Bot;

public static class BotTime
{
    private static readonly TimeZoneInfo MoscowZone = ResolveMoscowZone();

    public static DateTime MoscowToday =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowZone).Date;

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
