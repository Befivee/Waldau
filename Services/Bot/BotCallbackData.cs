namespace WaldauCastle.Services.Bot;

public static class BotCallbackData
{
    public const string MenuMain = "menu:main";
    public const string MenuBookings = "menu:bookings";
    public const string MenuEvents = "menu:events";
    public const string MenuExcursions = "menu:excursions";
    public const string MenuStats = "menu:stats";

    public const string PagePrev = "page:prev";
    public const string PageNext = "page:next";

    public const string EventAdd = "evt:add";
    public const string EventBackList = "evt:back_list";

    public const string ExcursionAdd = "exc:add";
    public const string ExcursionBackList = "exc:back_list";

    public static string EventView(int id) => $"evt:view:{id}";
    public static string EventEditTitle(int id) => $"evt:edit_title:{id}";
    public static string EventEditDescription(int id) => $"evt:edit_desc:{id}";
    public static string EventEditImage(int id) => $"evt:edit_img:{id}";
    public static string EventDelete(int id) => $"evt:del:{id}";
    public static string EventDeleteYes(int id) => $"evt:del_yes:{id}";
    public static string EventDeleteNo(int id) => $"evt:del_no:{id}";

    public static string ExcursionView(int id) => $"exc:view:{id}";
    public static string ExcursionEditTitle(int id) => $"exc:edit_title:{id}";
    public static string ExcursionEditDescription(int id) => $"exc:edit_desc:{id}";
    public static string ExcursionEditDuration(int id) => $"exc:edit_dur:{id}";
    public static string ExcursionEditPrice(int id) => $"exc:edit_price:{id}";
    public static string ExcursionEditImage(int id) => $"exc:edit_img:{id}";
    public static string ExcursionDelete(int id) => $"exc:del:{id}";
    public static string ExcursionDeleteYes(int id) => $"exc:del_yes:{id}";
    public static string ExcursionDeleteNo(int id) => $"exc:del_no:{id}";

    public static string BookingDelete(int id) => $"book:del:{id}";
    public static string BookingDeleteYes(int id) => $"book:del_yes:{id}";
    public static string BookingDeleteNo(int id) => $"book:del_no:{id}";

    public static bool TryParseEventId(string data, string prefix, out int eventId) =>
        TryParseId(data, prefix, out eventId);

    public static bool TryParseExcursionId(string data, string prefix, out int excursionId) =>
        TryParseId(data, prefix, out excursionId);

    public static bool TryParseBookingId(string data, string prefix, out int bookingId) =>
        TryParseId(data, prefix, out bookingId);

    private static bool TryParseId(string data, string prefix, out int id)
    {
        id = 0;
        if (!data.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        return int.TryParse(data[prefix.Length..], out id);
    }
}
