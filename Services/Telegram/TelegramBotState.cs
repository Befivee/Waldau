namespace WaldauCastle.Services.Telegram;

public enum TelegramBotState
{
    None,
    WaitingForEventTitle,
    WaitingForEventDescription,
    WaitingForEventDate,
    WaitingForEventImage,
    WaitingForNewTitle,
    WaitingForNewDescription,
    WaitingForNewDate,
    WaitingForNewImage
}
