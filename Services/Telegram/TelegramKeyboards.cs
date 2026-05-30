using Telegram.Bot.Types.ReplyMarkups;
using WaldauCastle.Models;

namespace WaldauCastle.Services.Telegram;

public static class TelegramKeyboards
{
    public static InlineKeyboardMarkup MainMenu() =>
        new([
            [InlineKeyboardButton.WithCallbackData("📋 Заявки", TelegramCallbackData.MenuBookings)],
            [InlineKeyboardButton.WithCallbackData("🎭 Ивенты", TelegramCallbackData.MenuEvents)],
            [InlineKeyboardButton.WithCallbackData("📊 Статистика", TelegramCallbackData.MenuStats)]
        ]);

    public static InlineKeyboardMarkup EventsList(IReadOnlyList<Event> events)
    {
        var rows = events
            .Select(e => new[] { InlineKeyboardButton.WithCallbackData(Truncate(e.Title, 40), TelegramCallbackData.EventView(e.Id)) })
            .ToList();

        rows.Add([InlineKeyboardButton.WithCallbackData("➕ Добавить ивент", TelegramCallbackData.EventAdd)]);
        rows.Add([InlineKeyboardButton.WithCallbackData("⬅ Главное меню", TelegramCallbackData.MenuMain)]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup EventManagement(int eventId) =>
        new([
            [InlineKeyboardButton.WithCallbackData("✏ Изменить название", TelegramCallbackData.EventEditTitle(eventId))],
            [InlineKeyboardButton.WithCallbackData("📝 Изменить описание", TelegramCallbackData.EventEditDescription(eventId))],
            [InlineKeyboardButton.WithCallbackData("🖼 Изменить изображение", TelegramCallbackData.EventEditImage(eventId))],
            [InlineKeyboardButton.WithCallbackData("🗑 Удалить", TelegramCallbackData.EventDelete(eventId))],
            [InlineKeyboardButton.WithCallbackData("⬅ Назад", TelegramCallbackData.EventBackList)]
        ]);

    public static InlineKeyboardMarkup DeleteConfirmation(int eventId) =>
        new([
            [
                InlineKeyboardButton.WithCallbackData("✅ Да", TelegramCallbackData.EventDeleteYes(eventId)),
                InlineKeyboardButton.WithCallbackData("❌ Нет", TelegramCallbackData.EventDeleteNo(eventId))
            ]
        ]);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
