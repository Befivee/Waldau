namespace WaldauCastle.Services.Telegram;

public enum TelegramBotState
{
    None,
    WaitingForEventTitle,
    WaitingForEventDescription,
    WaitingForEventDate,
    WaitingForEventImage,
    WaitingForCollaboration,
    WaitingForNewTitle,
    WaitingForNewDescription,
    WaitingForNewDate,
    WaitingForNewImage
}
