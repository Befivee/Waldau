using System.Globalization;

namespace WaldauCastle.Services.Bot;

public static class BotTextCommandResolver
{
    public static bool TryResolve(
        string? text,
        BotScreen screen,
        IReadOnlyList<int> pageIds,
        out string payload)
    {
        payload = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Trim();

        if (normalized == BotReplyLabels.BackMain)
        {
            payload = BotCallbackData.MenuMain;
            return true;
        }

        if (normalized == BotReplyLabels.Back)
        {
            payload = screen switch
            {
                BotScreen.EventDetail => BotCallbackData.EventBackList,
                BotScreen.ExcursionDetail => BotCallbackData.ExcursionBackList,
                _ => BotCallbackData.MenuMain
            };
            return true;
        }

        if (normalized == BotReplyLabels.Add)
        {
            payload = screen switch
            {
                BotScreen.Events => BotCallbackData.EventAdd,
                BotScreen.Excursions => BotCallbackData.ExcursionAdd,
                _ => string.Empty
            };
            return payload.Length > 0;
        }

        if (normalized == BotReplyLabels.Prev)
        {
            payload = BotCallbackData.PagePrev;
            return screen is BotScreen.Bookings or BotScreen.Events or BotScreen.Excursions;
        }

        if (normalized == BotReplyLabels.Next)
        {
            payload = BotCallbackData.PageNext;
            return screen is BotScreen.Bookings or BotScreen.Events or BotScreen.Excursions;
        }

        if (normalized == BotReplyLabels.Yes || normalized == BotReplyLabels.No)
            return false;

        if (int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
            index is >= 1 and <= BotListPaging.PageSize)
        {
            payload = screen switch
            {
                BotScreen.Bookings when pageIds.Count >= index =>
                    BotCallbackData.BookingDelete(pageIds[index - 1]),
                BotScreen.Events when pageIds.Count >= index =>
                    BotCallbackData.EventView(pageIds[index - 1]),
                BotScreen.Excursions when pageIds.Count >= index =>
                    BotCallbackData.ExcursionView(pageIds[index - 1]),
                BotScreen.EventDetail when pageIds.Count > 0 => index switch
                {
                    1 => BotCallbackData.EventEditTitle(pageIds[0]),
                    2 => BotCallbackData.EventEditDescription(pageIds[0]),
                    3 => BotCallbackData.EventEditImage(pageIds[0]),
                    4 => BotCallbackData.EventDelete(pageIds[0]),
                    _ => string.Empty
                },
                BotScreen.ExcursionDetail when pageIds.Count > 0 => index switch
                {
                    1 => BotCallbackData.ExcursionEditTitle(pageIds[0]),
                    2 => BotCallbackData.ExcursionEditDescription(pageIds[0]),
                    3 => BotCallbackData.ExcursionEditDuration(pageIds[0]),
                    4 => BotCallbackData.ExcursionEditPrice(pageIds[0]),
                    5 => BotCallbackData.ExcursionEditImage(pageIds[0]),
                    6 => BotCallbackData.ExcursionDelete(pageIds[0]),
                    _ => string.Empty
                },
                BotScreen.Main or BotScreen.None => index switch
                {
                    1 => BotCallbackData.MenuBookings,
                    2 => BotCallbackData.MenuEvents,
                    3 => BotCallbackData.MenuExcursions,
                    4 => BotCallbackData.MenuStats,
                    _ => string.Empty
                },
                _ => string.Empty
            };

            return payload.Length > 0;
        }

        if (screen is BotScreen.Main or BotScreen.None)
        {
            payload = normalized switch
            {
                "1" => BotCallbackData.MenuBookings,
                "2" => BotCallbackData.MenuEvents,
                "3" => BotCallbackData.MenuExcursions,
                "4" => BotCallbackData.MenuStats,
                _ => string.Empty
            };
            return payload.Length > 0;
        }

        return false;
    }

    public static bool TryResolveConfirmation(string? text, out bool confirmed)
    {
        confirmed = false;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Trim();
        if (normalized == BotReplyLabels.Yes)
        {
            confirmed = true;
            return true;
        }

        if (normalized == BotReplyLabels.No)
        {
            confirmed = false;
            return true;
        }

        return false;
    }
}
