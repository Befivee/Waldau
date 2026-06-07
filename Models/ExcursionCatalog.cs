namespace WaldauCastle.Models;

public sealed class ExcursionTypeInfo
{
    public ExcursionKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string ImagePath { get; init; } = Excursion.DefaultImagePath;
    public bool RequiresTimeSlot { get; init; }

    public int Id => (int)Kind;
    public string KindKey => Kind == ExcursionKind.Guided ? "guided" : "self";
    public string FormLabel => Kind == ExcursionKind.Guided ? "С гидом" : "Самостоятельно";
    public string DisplayPrice => $"от {Price:0} ₽";
}

public static class ExcursionCatalog
{
    public static readonly ExcursionTypeInfo Guided = new()
    {
        Kind = ExcursionKind.Guided,
        Title = "Экскурсия с гидом",
        Description =
            "Прогулка по замку с профессиональным гидом: история крепости, " +
            "интерьеры, легенды и ответы на ваши вопросы.",
        Duration = "90 мин",
        Price = 650,
        ImagePath = "/images/All/Экскурсии.webp",
        RequiresTimeSlot = true
    };

    public static readonly ExcursionTypeInfo SelfGuided = new()
    {
        Kind = ExcursionKind.SelfGuided,
        Title = "Самостоятельное посещение",
        Description =
            "Свободный осмотр замка в часы работы: экспозиции, двор, " +
            "фотозоны и атмосфера средневековой крепости без сопровождения гида.",
        Duration = "без ограничения",
        Price = 650,
        ImagePath = "/images/hero/home-hero.webp",
        RequiresTimeSlot = false
    };

    public static IReadOnlyList<ExcursionTypeInfo> All { get; } = [Guided, SelfGuided];

    public static string[] GuidedTimeSlots { get; } =
        ["10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00"];

    public static bool TryGetById(int? id, out ExcursionTypeInfo info)
    {
        info = All.FirstOrDefault(item => item.Id == id)!;
        return info is not null;
    }

    public static ExcursionTypeInfo Get(ExcursionKind kind) =>
        kind == ExcursionKind.Guided ? Guided : SelfGuided;
}
