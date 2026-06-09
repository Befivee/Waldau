namespace WaldauCastle.Models;

public sealed class ExcursionTypeInfo
{
    public ExcursionKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public decimal RegularPrice { get; init; }
    public decimal ConcessionPrice { get; init; } = ExcursionCatalog.ConcessionPrice;
    public string ImagePath { get; init; } = Excursion.DefaultImagePath;
    public bool RequiresTimeSlot { get; init; }

    public int Id => (int)Kind;
    public string KindKey => Kind == ExcursionKind.Guided ? "guided" : "self";
    public string FormLabel => Kind == ExcursionKind.Guided ? "С гидом" : "Самостоятельно";
    public string DisplayPrice => $"от {ConcessionPrice:0} ₽";
    public string PriceDetail => $"{RegularPrice:0} ₽ / {ConcessionPrice:0} ₽ льготный";
    public string FormPriceLabel => $"{RegularPrice:0} ₽ / {ConcessionPrice:0} ₽ льгот.";
}

public static class ExcursionCatalog
{
    public const decimal ConcessionPrice = 650;
    public const decimal GuidedRegularPrice = 1000;
    public const decimal SelfGuidedRegularPrice = 800;

    public static readonly ExcursionTypeInfo Guided = new()
    {
        Kind = ExcursionKind.Guided,
        Title = "Экскурсия с гидом",
        Description =
            "Прогулка по замку с профессиональным гидом: история крепости, " +
            "интерьеры, легенды и ответы на ваши вопросы.",
        Duration = "40 мин",
        RegularPrice = GuidedRegularPrice,
        ImagePath = "/images/excursion-guided.webp",
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
        RegularPrice = SelfGuidedRegularPrice,
        ImagePath = "/images/excursion-self.webp",
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
