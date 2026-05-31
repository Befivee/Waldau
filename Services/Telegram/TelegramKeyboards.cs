using Telegram.Bot.Types.ReplyMarkups;
using WaldauCastle.Models;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.Telegram;

public static class TelegramKeyboards
{
    public static ReplyKeyboardMarkup MainMenu() => new([
        [
            new KeyboardButton(BotReplyLabels.Number(1)),
            new KeyboardButton(BotReplyLabels.Number(2)),
            new KeyboardButton(BotReplyLabels.Number(3)),
            new KeyboardButton(BotReplyLabels.Number(4))
        ]
    ])
    {
        ResizeKeyboard = true
    };

    public static ReplyKeyboardMarkup BookingsPage(IReadOnlyList<Booking> bookings, int page, int totalPages)
    {
        var rows = new List<KeyboardButton[]>();
        if (bookings.Count > 0)
        {
            rows.Add(bookings
                .Select((_, i) => new KeyboardButton(BotReplyLabels.Number(i + 1)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([new KeyboardButton(BotReplyLabels.BackMain)]);
        return Build(rows);
    }

    public static ReplyKeyboardMarkup EventsPage(IReadOnlyList<Event> events, int page, int totalPages)
    {
        var rows = new List<KeyboardButton[]>();
        if (events.Count > 0)
        {
            rows.Add(events
                .Select((_, i) => new KeyboardButton(BotReplyLabels.Number(i + 1)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([new KeyboardButton(BotReplyLabels.Add)]);
        rows.Add([new KeyboardButton(BotReplyLabels.BackMain)]);
        return Build(rows);
    }

    public static ReplyKeyboardMarkup ExcursionsPage(IReadOnlyList<Excursion> excursions, int page, int totalPages)
    {
        var rows = new List<KeyboardButton[]>();
        if (excursions.Count > 0)
        {
            rows.Add(excursions
                .Select((_, i) => new KeyboardButton(BotReplyLabels.Number(i + 1)))
                .ToArray());
        }

        rows.AddRange(NavigationRow(page, totalPages));
        rows.Add([new KeyboardButton(BotReplyLabels.Add)]);
        rows.Add([new KeyboardButton(BotReplyLabels.BackMain)]);
        return Build(rows);
    }

    public static ReplyKeyboardMarkup EventManagement() => Build([
        [
            new KeyboardButton(BotReplyLabels.Number(1)),
            new KeyboardButton(BotReplyLabels.Number(2)),
            new KeyboardButton(BotReplyLabels.Number(3)),
            new KeyboardButton(BotReplyLabels.Number(4))
        ],
        [new KeyboardButton(BotReplyLabels.Back)]
    ]);

    public static ReplyKeyboardMarkup ExcursionManagement() => Build([
        [
            new KeyboardButton(BotReplyLabels.Number(1)),
            new KeyboardButton(BotReplyLabels.Number(2)),
            new KeyboardButton(BotReplyLabels.Number(3)),
            new KeyboardButton(BotReplyLabels.Number(4))
        ],
        [new KeyboardButton("5"), new KeyboardButton("6")],
        [new KeyboardButton(BotReplyLabels.Back)]
    ]);

    public static ReplyKeyboardMarkup DeleteConfirmation() => Build([
        [new KeyboardButton(BotReplyLabels.Yes), new KeyboardButton(BotReplyLabels.No)]
    ]);

    public static ReplyKeyboardMarkup BackToMainMenu() => Build([
        [new KeyboardButton(BotReplyLabels.BackMain)]
    ]);

    public static ReplyKeyboardRemove Remove() => new();

    private static IEnumerable<KeyboardButton[]> NavigationRow(int page, int totalPages)
    {
        if (totalPages <= 1)
            yield break;

        var buttons = new List<KeyboardButton>();
        if (page > 0)
            buttons.Add(new KeyboardButton(BotReplyLabels.Prev));
        if (page < totalPages - 1)
            buttons.Add(new KeyboardButton(BotReplyLabels.Next));

        if (buttons.Count > 0)
            yield return buttons.ToArray();
    }

    private static ReplyKeyboardMarkup Build(IEnumerable<KeyboardButton[]> rows) => new(rows)
    {
        ResizeKeyboard = true
    };
}
