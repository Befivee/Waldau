using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.VK;

public class VKUserSession
{
    public VKBotState State { get; set; } = VKBotState.None;

    public BotScreen Screen { get; set; } = BotScreen.None;

    public int ListPage { get; set; }

    public int? EventId { get; set; }

    public int? ExcursionId { get; set; }

    public int? PendingDeleteBookingId { get; set; }

    public int? PendingDeleteEventId { get; set; }

    public int? PendingDeleteExcursionId { get; set; }

    public List<int> PageIds { get; set; } = [];

    public string? DraftTitle { get; set; }

    public string? DraftDescription { get; set; }

    public DateTime? DraftEventDate { get; set; }

    public string? DraftDuration { get; set; }

    public decimal? DraftPrice { get; set; }

    public void Reset()
    {
        State = VKBotState.None;
        Screen = BotScreen.None;
        ListPage = 0;
        EventId = null;
        ExcursionId = null;
        PendingDeleteBookingId = null;
        PendingDeleteEventId = null;
        PendingDeleteExcursionId = null;
        PageIds = [];
        DraftTitle = null;
        DraftDescription = null;
        DraftEventDate = null;
        DraftDuration = null;
        DraftPrice = null;
    }
}
