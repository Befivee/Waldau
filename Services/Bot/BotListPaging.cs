namespace WaldauCastle.Services.Bot;

public static class BotListPaging
{
    public const int PageSize = 4;

    public static int TotalPages(int itemCount) =>
        itemCount <= 0 ? 1 : (itemCount + PageSize - 1) / PageSize;

    public static IReadOnlyList<T> GetPage<T>(IReadOnlyList<T> items, int page)
    {
        if (items.Count == 0)
            return [];

        var safePage = Math.Clamp(page, 0, TotalPages(items.Count) - 1);
        return items.Skip(safePage * PageSize).Take(PageSize).ToList();
    }
}
