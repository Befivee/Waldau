using WaldauCastle.Models;

namespace WaldauCastle.Services;

public static class BookingNotificationText
{
    public const string EventBookingPrefix = "Запись на мероприятие:";

    public static string Format(Booking booking)
    {
        var culture = new System.Globalization.CultureInfo("ru-RU");
        var schedule = string.IsNullOrWhiteSpace(booking.VisitTime)
            ? booking.VisitDate.ToString("d MMMM yyyy", culture)
            : $"{booking.VisitDate.ToString("d MMMM yyyy", culture)}, {booking.VisitTime}";

        var typeLine = booking.ExcursionTitle.StartsWith(EventBookingPrefix, StringComparison.Ordinal)
            ? booking.ExcursionTitle
            : $"Экскурсия: {booking.ExcursionTitle}";

        return
            "🔔 Новая заявка\n\n" +
            $"{typeLine}\n" +
            $"ФИО: {booking.FullName}\n" +
            $"Телефон: {booking.Phone}\n" +
            $"Дата: {schedule}\n" +
            $"Количество человек: {booking.PersonsCount}";
    }
}
