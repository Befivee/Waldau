namespace WaldauCastle.Services.Bot;

public static class BotReplyLabels
{
    public const string BackMain = "⬅ Главное меню";
    public const string Back = "⬅ Назад";
    public const string Add = "➕ Добавить";
    public const string Prev = "◀";
    public const string Next = "▶";
    public const string Yes = "✅ Да";
    public const string No = "❌ Нет";

    public static string Number(int index) => index.ToString();
}
