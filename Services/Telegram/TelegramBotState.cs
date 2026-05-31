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
    WaitingForNewImage,
    WaitingForExcursionTitle,
    WaitingForExcursionDescription,
    WaitingForExcursionDuration,
    WaitingForExcursionPrice,
    WaitingForNewExcursionTitle,
    WaitingForNewExcursionDescription,
    WaitingForNewExcursionDuration,
    WaitingForNewExcursionPrice,
    WaitingForExcursionImage,
    WaitingForNewExcursionImage
}
