using System.Text.Json;
using WaldauCastle.Models;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.VK;

public static class VKKeyboards
{
    public static string MainMenu() =>
        BuildReply(
        [
            [
                Callback(BotReplyLabels.Number(1), BotCallbackData.MenuBookings),
                Callback(BotReplyLabels.Number(2), BotCallbackData.MenuEvents),
                Callback(BotReplyLabels.Number(3), BotCallbackData.MenuExcursions),
                Callback(BotReplyLabels.Number(4), BotCallbackData.MenuStats)
            ]
        ]);

    public static string BookingsPage(IReadOnlyList<Booking> bookings, int page, int totalPages)
    {
        var rows = new List<object[]>();
        if (bookings.Count > 0)
        {
            rows.Add(bookings
                .Select((b, i) => Callback(BotReplyLabels.Number(i + 1), BotCallbackData.BookingDelete(b.Id)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([Callback(BotReplyLabels.BackMain, BotCallbackData.MenuMain)]);
        return BuildReply(rows);
    }

    public static string EventsPage(IReadOnlyList<Event> events, int page, int totalPages)
    {
        var rows = new List<object[]>();
        if (events.Count > 0)
        {
            rows.Add(events
                .Select((e, i) => Callback(BotReplyLabels.Number(i + 1), BotCallbackData.EventView(e.Id)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([Callback(BotReplyLabels.Add, BotCallbackData.EventAdd)]);
        rows.Add([Callback(BotReplyLabels.BackMain, BotCallbackData.MenuMain)]);
        return BuildReply(rows);
    }

    public static string ExcursionsPage(IReadOnlyList<Excursion> excursions, int page, int totalPages)
    {
        var rows = new List<object[]>();
        if (excursions.Count > 0)
        {
            rows.Add(excursions
                .Select((e, i) => Callback(BotReplyLabels.Number(i + 1), BotCallbackData.ExcursionView(e.Id)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([Callback(BotReplyLabels.Add, BotCallbackData.ExcursionAdd)]);
        rows.Add([Callback(BotReplyLabels.BackMain, BotCallbackData.MenuMain)]);
        return BuildReply(rows);
    }

    public static string EventManagement(int eventId) =>
        BuildReply(
        [
            [
                Callback(BotReplyLabels.Number(1), BotCallbackData.EventEditTitle(eventId)),
                Callback(BotReplyLabels.Number(2), BotCallbackData.EventEditDescription(eventId)),
                Callback(BotReplyLabels.Number(3), BotCallbackData.EventEditImage(eventId)),
                Callback(BotReplyLabels.Number(4), BotCallbackData.EventDelete(eventId))
            ],
            [Callback(BotReplyLabels.Back, BotCallbackData.EventBackList)]
        ]);

    public static string ExcursionManagement(int excursionId) =>
        BuildReply(
        [
            [
                Callback(BotReplyLabels.Number(1), BotCallbackData.ExcursionEditTitle(excursionId)),
                Callback(BotReplyLabels.Number(2), BotCallbackData.ExcursionEditDescription(excursionId)),
                Callback(BotReplyLabels.Number(3), BotCallbackData.ExcursionEditDuration(excursionId)),
                Callback(BotReplyLabels.Number(4), BotCallbackData.ExcursionEditPrice(excursionId))
            ],
            [
                Callback("5", BotCallbackData.ExcursionEditImage(excursionId)),
                Callback("6", BotCallbackData.ExcursionDelete(excursionId))
            ],
            [Callback(BotReplyLabels.Back, BotCallbackData.ExcursionBackList)]
        ]);

    public static string DeleteConfirmation(string yesPayload, string noPayload) =>
        BuildReply(
        [
            [
                Callback(BotReplyLabels.Yes, yesPayload),
                Callback(BotReplyLabels.No, noPayload)
            ]
        ]);

    public static string BackToMainMenu() =>
        BuildReply([[Callback(BotReplyLabels.BackMain, BotCallbackData.MenuMain)]]);

    public static string Remove() =>
        JsonSerializer.Serialize(new { buttons = Array.Empty<object[]>(), one_time = true });

    private static IEnumerable<object[]> NavigationRow(int page, int totalPages)
    {
        if (totalPages <= 1)
            yield break;

        var buttons = new List<object>();
        if (page > 0)
            buttons.Add(Callback(BotReplyLabels.Prev, BotCallbackData.PagePrev));
        if (page < totalPages - 1)
            buttons.Add(Callback(BotReplyLabels.Next, BotCallbackData.PageNext));

        if (buttons.Count > 0)
            yield return buttons.ToArray();
    }

    private static object Callback(string label, string payload) => new
    {
        action = new
        {
            type = "callback",
            label,
            payload = JsonSerializer.Serialize(new { cmd = payload })
        },
        color = "secondary"
    };

    private static string BuildReply(IEnumerable<IEnumerable<object>> rows)
    {
        var keyboard = new
        {
            one_time = false,
            inline = false,
            buttons = rows.Select(r => r.ToArray()).ToArray()
        };

        return JsonSerializer.Serialize(keyboard);
    }
}
