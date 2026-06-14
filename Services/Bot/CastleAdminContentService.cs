using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WaldauCastle.Data;
using WaldauCastle.Models;
using WaldauCastle.Services;

namespace WaldauCastle.Services.Bot;

public class CastleAdminContentService(
    IBookingService bookings,
    ApplicationDbContext db)
{
    private static readonly CultureInfo RuCulture = new("ru-RU");

    public async Task<(string Text, int TotalPages)> BuildBookingsPageAsync(
        int page,
        CancellationToken cancellationToken)
    {
        var total = await bookings.GetCountAsync(cancellationToken);
        var totalPages = BotListPaging.TotalPages(total);
        var safePage = Math.Clamp(page, 0, totalPages - 1);
        var list = await bookings.GetPageAsync(safePage, BotListPaging.PageSize, cancellationToken);

        if (total == 0)
            return ("📋 Заявок пока нет.", 1);

        var lines = new List<string>
        {
            $"📋 Заявки (стр. {safePage + 1}/{totalPages})\n"
        };

        for (var i = 0; i < list.Count; i++)
        {
            var booking = list[i];
            var schedule = string.IsNullOrWhiteSpace(booking.VisitTime)
                ? booking.VisitDate.ToString("d MMMM yyyy", RuCulture)
                : $"{booking.VisitDate.ToString("d MMMM yyyy", RuCulture)}, {booking.VisitTime}";

            lines.Add(
                $"{i + 1}. {booking.FullName}\n" +
                $"   {booking.ExcursionTitle}\n" +
                $"   {booking.Phone}\n" +
                $"   {schedule}, {booking.PersonsCount} чел.");
        }

        lines.Add("\nНажмите номер заявки, чтобы удалить.");

        return (string.Join("\n\n", lines), totalPages);
    }

    public async Task<string> BuildStatisticsTextAsync(CancellationToken cancellationToken)
    {
        var eventsCount = await db.Events.CountAsync(cancellationToken);
        var bookingsCount = await db.Bookings.CountAsync(cancellationToken);
        var upcomingCount = await db.Events.CountAsync(e => e.EventDate >= DateTime.Today, cancellationToken);

        return
            "📊 Статистика\n\n" +
            $"🎭 Мероприятий: {eventsCount} (предстоящих: {upcomingCount})\n" +
            $"📋 Заявок: {bookingsCount}\n" +
            $"🚶 Форматов экскурсий: {ExcursionCatalog.All.Count}";
    }

    public Task<string> BuildExcursionsTextAsync(CancellationToken cancellationToken)
    {
        var lines = ExcursionCatalog.All.Select(e =>
            $"• {e.Title}\n  ⏱ {e.Duration} · 💰 {e.DisplayPrice}\n  {Truncate(e.Description, 120)}");

        return Task.FromResult("🚶 Экскурсии:\n\n" + string.Join("\n\n", lines));
    }

    public static string BuildNumberedEventsIntro(IReadOnlyList<Event> events, int page)
    {
        var paged = BotListPaging.GetPage(events, page);
        var totalPages = BotListPaging.TotalPages(events.Count);
        var lines = new List<string> { $"🎭 Мероприятия (стр. {page + 1}/{totalPages}):" };

        for (var i = 0; i < paged.Count; i++)
            lines.Add($"{i + 1}. {paged[i].Title}");

        return string.Join('\n', lines);
    }

    public static string BuildNumberedExcursionsIntro(IReadOnlyList<Excursion> items, int page)
    {
        var paged = BotListPaging.GetPage(items, page);
        var totalPages = BotListPaging.TotalPages(items.Count);
        var lines = new List<string> { $"🚶 Экскурсии (стр. {page + 1}/{totalPages}):" };

        for (var i = 0; i < paged.Count; i++)
            lines.Add($"{i + 1}. {paged[i].Title}");

        return string.Join('\n', lines);
    }

    public string BuildEventDetailsText(Event entity) =>
        $"🎭 {entity.Title}\n\n" +
        $"📅 {entity.EventDate.ToString("d MMMM yyyy", RuCulture)}\n\n" +
        $"{entity.Description}";

    public string BuildExcursionDetailsText(Excursion entity) =>
        $"🚶 {entity.Title}\n\n" +
        $"⏱ {entity.Duration}\n" +
        $"💰 {entity.Price:0} ₽\n\n" +
        $"{entity.Description}";

    public string BuildBookingDeletePrompt(Booking booking)
    {
        var visitSchedule = FormatVisitSchedule(booking);

        return
            $"📅 Заявка получена: {BotTime.FormatLocalDateTime(booking.CreatedAt)}\n\n" +
            $"🗑 Удалить заявку «{booking.FullName}» ({visitSchedule})?";
    }

    private string FormatVisitSchedule(Booking booking) =>
        string.IsNullOrWhiteSpace(booking.VisitTime)
            ? booking.VisitDate.ToString("d MMMM yyyy", RuCulture)
            : $"{booking.VisitDate.ToString("d MMMM yyyy", RuCulture)}, {booking.VisitTime}";

    public static string BuildPublicWelcomeText(string siteUrl) =>
        "🏰 Замок Вальдау\n\n" +
        "Напишите «экскурсии» — список программ с сайта.\n" +
        $"Запись онлайн: {siteUrl}";

    public static bool IsExcursionsRequest(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Trim().ToLowerInvariant();
        return normalized is "экскурсии" or "экскурсия" or "/excursions" or "excursions";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
