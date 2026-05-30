namespace WaldauCastle.Services.Telegram;

public static class TelegramCallbackData
{
    public const string MenuMain = "menu:main";
    public const string MenuBookings = "menu:bookings";
    public const string MenuEvents = "menu:events";
    public const string MenuStats = "menu:stats";

    public const string EventsList = "evt:list";
    public const string EventAdd = "evt:add";
    public const string EventBackList = "evt:back_list";

    public static string EventView(int id) => $"evt:view:{id}";
    public static string EventEditTitle(int id) => $"evt:edit_title:{id}";
    public static string EventEditDescription(int id) => $"evt:edit_desc:{id}";
    public static string EventEditImage(int id) => $"evt:edit_img:{id}";
    public static string EventDelete(int id) => $"evt:del:{id}";
    public static string EventDeleteYes(int id) => $"evt:del_yes:{id}";
    public static string EventDeleteNo(int id) => $"evt:del_no:{id}";

    public static bool TryParseEventId(string data, string prefix, out int eventId)
    {
        eventId = 0;
        if (!data.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        return int.TryParse(data[prefix.Length..], out eventId);
    }
}
