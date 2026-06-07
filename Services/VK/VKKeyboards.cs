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
                Text(BotReplyLabels.Number(1)),
                Text(BotReplyLabels.Number(2)),
                Text(BotReplyLabels.Number(3))
            ]
        ]);

    public static string BookingsPage(IReadOnlyList<Booking> bookings, int page, int totalPages)
    {
        var rows = new List<object[]>();
        if (bookings.Count > 0)
        {
            rows.Add(bookings
                .Select((_, i) => Text(BotReplyLabels.Number(i + 1)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([Text(BotReplyLabels.BackMain)]);
        return BuildReply(rows);
    }

    public static string EventsPage(IReadOnlyList<Event> events, int page, int totalPages)
    {
        var rows = new List<object[]>();
        if (events.Count > 0)
        {
            rows.Add(events
                .Select((_, i) => Text(BotReplyLabels.Number(i + 1)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([Text(BotReplyLabels.Add)]);
        rows.Add([Text(BotReplyLabels.BackMain)]);
        return BuildReply(rows);
    }

    public static string EventManagement(int eventId) =>
        BuildReply(
        [
            [
                Text(BotReplyLabels.Number(1)),
                Text(BotReplyLabels.Number(2)),
                Text(BotReplyLabels.Number(3)),
                Text(BotReplyLabels.Number(4))
            ],
            [Text(BotReplyLabels.Back)]
        ]);

    public static string DeleteConfirmation() =>
        BuildReply(
        [
            [
                Text(BotReplyLabels.Yes),
                Text(BotReplyLabels.No)
            ]
        ]);

    public static string BackToMainMenu() =>
        BuildReply([[Text(BotReplyLabels.BackMain)]]);

    public static string Remove() =>
        JsonSerializer.Serialize(new { buttons = Array.Empty<object[]>(), one_time = true });

    private static IEnumerable<object[]> NavigationRow(int page, int totalPages)
    {
        if (totalPages <= 1)
            yield break;

        var buttons = new List<object>();
        if (page > 0)
            buttons.Add(Text(BotReplyLabels.Prev));
        if (page < totalPages - 1)
            buttons.Add(Text(BotReplyLabels.Next));

        if (buttons.Count > 0)
            yield return buttons.ToArray();
    }

    private static object Text(string label) => new
    {
        action = new
        {
            type = "text",
            label,
            payload = JsonSerializer.Serialize(new { cmd = label })
        },
        color = "primary"
    };

    private static string BuildReply(IEnumerable<IEnumerable<object>> rows)
    {
        var keyboard = new
        {
            one_time = false,
            buttons = rows.Select(r => r.ToArray()).ToArray()
        };

        return JsonSerializer.Serialize(keyboard);
    }
}
