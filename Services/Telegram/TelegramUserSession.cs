namespace WaldauCastle.Services.Telegram;

public class TelegramUserSession
{
    public TelegramBotState State { get; set; } = TelegramBotState.None;

    public int? EventId { get; set; }

    public string? DraftTitle { get; set; }

    public string? DraftDescription { get; set; }

    public DateTime? DraftEventDate { get; set; }

    public void Reset()
    {
        State = TelegramBotState.None;
        EventId = null;
        DraftTitle = null;
        DraftDescription = null;
        DraftEventDate = null;
    }

    public void ResetDraft()
    {
        DraftTitle = null;
        DraftDescription = null;
        DraftEventDate = null;
    }
}
