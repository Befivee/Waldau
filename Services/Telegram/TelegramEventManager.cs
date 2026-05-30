using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WaldauCastle.Data;
using WaldauCastle.Models;
using WaldauCastle.Services;

namespace WaldauCastle.Services.Telegram;

public class TelegramEventManager(
    IEventService events,
    IEventImageService images,
    IBookingService bookings,
    ApplicationDbContext db,
    TelegramStateService stateService,
    ILogger<TelegramEventManager> logger)
{
    private static readonly CultureInfo RuCulture = new("ru-RU");

    public async Task SendMainMenuAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        stateService.GetOrCreate(chatId).Reset();
        await bot.SendMessage(
            chatId,
            "🏰 Панель управления замком Вальдау\n\nВыберите раздел:",
            replyMarkup: TelegramKeyboards.MainMenu(),
            cancellationToken: cancellationToken);
    }

    public async Task SendBookingsAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var list = await bookings.GetLatestAsync(10, cancellationToken);

        if (list.Count == 0)
        {
            await bot.SendMessage(chatId, "📋 Заявок пока нет.", replyMarkup: BackToMainMenu(), cancellationToken: cancellationToken);
            return;
        }

        var lines = list.Select(b =>
            $"• {b.FullName}\n  {b.Phone}\n  {b.VisitDate.ToString("d MMMM yyyy", RuCulture)}\n  {b.PersonsCount} чел.");

        await bot.SendMessage(
            chatId,
            "📋 Последние заявки:\n\n" + string.Join("\n\n", lines),
            replyMarkup: BackToMainMenu(),
            cancellationToken: cancellationToken);
    }

    public async Task SendStatisticsAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var eventsCount = await db.Events.CountAsync(cancellationToken);
        var bookingsCount = await db.Bookings.CountAsync(cancellationToken);
        var excursionsCount = await db.Excursions.CountAsync(cancellationToken);
        var upcomingCount = await db.Events.CountAsync(e => e.EventDate >= DateTime.Today, cancellationToken);

        var text =
            "📊 Статистика\n\n" +
            $"🎭 Мероприятий: {eventsCount} (предстоящих: {upcomingCount})\n" +
            $"📋 Заявок: {bookingsCount}\n" +
            $"🚶 Экскурсий: {excursionsCount}";

        await bot.SendMessage(chatId, text, replyMarkup: BackToMainMenu(), cancellationToken: cancellationToken);
    }

    public async Task SendEventsListAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        stateService.GetOrCreate(chatId).Reset();
        var list = await events.GetAllAsync(cancellationToken);

        if (list.Count == 0)
        {
            await bot.SendMessage(
                chatId,
                "🎭 Мероприятий пока нет.\n\nДобавьте первое событие:",
                replyMarkup: TelegramKeyboards.EventsList(list),
                cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(
            chatId,
            "🎭 Мероприятия — выберите для редактирования:",
            replyMarkup: TelegramKeyboards.EventsList(list),
            cancellationToken: cancellationToken);
    }

    public async Task SendEventDetailsAsync(
        ITelegramBotClient bot,
        long chatId,
        int eventId,
        CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is null)
        {
            await bot.SendMessage(chatId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            await SendEventsListAsync(bot, chatId, cancellationToken);
            return;
        }

        stateService.GetOrCreate(chatId).Reset();
        stateService.GetOrCreate(chatId).EventId = eventId;

        var text =
            $"🎭 {entity.Title}\n\n" +
            $"📅 {entity.EventDate.ToString("d MMMM yyyy", RuCulture)}\n\n" +
            $"{entity.Description}";

        await bot.SendMessage(
            chatId,
            text,
            replyMarkup: TelegramKeyboards.EventManagement(eventId),
            cancellationToken: cancellationToken);
    }

    public async Task StartAddWizardAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.Reset();
        session.State = TelegramBotState.WaitingForEventTitle;

        await bot.SendMessage(
            chatId,
            "➕ Новое мероприятие\n\nШаг 1 из 4\nВведите название:",
            cancellationToken: cancellationToken);
    }

    public async Task StartEditTitleAsync(ITelegramBotClient bot, long chatId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(bot, chatId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewTitle;
        session.EventId = eventId;

        await bot.SendMessage(chatId, "✏ Введите новое название:", cancellationToken: cancellationToken);
    }

    public async Task StartEditDescriptionAsync(ITelegramBotClient bot, long chatId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(bot, chatId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewDescription;
        session.EventId = eventId;

        await bot.SendMessage(chatId, "📝 Введите новое описание:", cancellationToken: cancellationToken);
    }

    public async Task StartEditImageAsync(ITelegramBotClient bot, long chatId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(bot, chatId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewImage;
        session.EventId = eventId;

        await bot.SendMessage(chatId, "🖼 Отправьте новое изображение:", cancellationToken: cancellationToken);
    }

    public async Task SendDeleteConfirmationAsync(
        ITelegramBotClient bot,
        long chatId,
        int eventId,
        CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is null)
        {
            await bot.SendMessage(chatId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(
            chatId,
            $"🗑 Удалить мероприятие «{entity.Title}»?",
            replyMarkup: TelegramKeyboards.DeleteConfirmation(eventId),
            cancellationToken: cancellationToken);
    }

    public async Task DeleteEventAsync(ITelegramBotClient bot, long chatId, int eventId, CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is null)
        {
            await bot.SendMessage(chatId, "Мероприятие уже удалено.", cancellationToken: cancellationToken);
            await SendEventsListAsync(bot, chatId, cancellationToken);
            return;
        }

        await images.DeleteIfUploadedAsync(entity.ImagePath, cancellationToken);
        await events.DeleteAsync(eventId, cancellationToken);
        stateService.GetOrCreate(chatId).Reset();

        await bot.SendMessage(chatId, "✅ Мероприятие удалено.", cancellationToken: cancellationToken);
        await SendEventsListAsync(bot, chatId, cancellationToken);
    }

    public async Task HandleTextMessageAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? string.Empty;
        var session = stateService.GetOrCreate(chatId);

        if (session.State == TelegramBotState.None)
        {
            await bot.SendMessage(chatId, "Используйте /start для открытия меню.", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            switch (session.State)
            {
                case TelegramBotState.WaitingForEventTitle:
                    await HandleWizardTitleAsync(bot, chatId, text, cancellationToken);
                    break;
                case TelegramBotState.WaitingForEventDescription:
                    await HandleWizardDescriptionAsync(bot, chatId, text, cancellationToken);
                    break;
                case TelegramBotState.WaitingForEventDate:
                    await HandleWizardDateAsync(bot, chatId, text, cancellationToken);
                    break;
                case TelegramBotState.WaitingForNewTitle:
                    await HandleEditTitleAsync(bot, chatId, text, cancellationToken);
                    break;
                case TelegramBotState.WaitingForNewDescription:
                    await HandleEditDescriptionAsync(bot, chatId, text, cancellationToken);
                    break;
                case TelegramBotState.WaitingForEventImage:
                case TelegramBotState.WaitingForNewImage:
                    await bot.SendMessage(chatId, "🖼 Отправьте изображение (фото), а не текст.", cancellationToken: cancellationToken);
                    break;
                default:
                    session.Reset();
                    await SendMainMenuAsync(bot, chatId, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обработки текста Telegram для chat {ChatId}", chatId);
            await bot.SendMessage(chatId, "⚠️ Не удалось обработать сообщение. Попробуйте снова или /start.", cancellationToken: cancellationToken);
        }
    }

    public async Task HandlePhotoMessageAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var session = stateService.GetOrCreate(chatId);

        if (session.State is not (TelegramBotState.WaitingForEventImage or TelegramBotState.WaitingForNewImage))
        {
            await bot.SendMessage(chatId, "Сейчас изображение не ожидается. Используйте /start.", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            if (session.State == TelegramBotState.WaitingForEventImage)
                await CompleteAddWizardAsync(bot, chatId, message, cancellationToken);
            else
                await CompleteEditImageAsync(bot, chatId, message, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await bot.SendMessage(chatId, $"⚠️ {ex.Message}", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка загрузки изображения Telegram для chat {ChatId}", chatId);
            await bot.SendMessage(chatId, "⚠️ Не удалось сохранить изображение. Попробуйте другое фото.", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleWizardTitleAsync(
        ITelegramBotClient bot,
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await bot.SendMessage(chatId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftTitle = text;
        session.State = TelegramBotState.WaitingForEventDescription;

        await bot.SendMessage(chatId, "Шаг 2 из 4\nВведите описание:", cancellationToken: cancellationToken);
    }

    private async Task HandleWizardDescriptionAsync(
        ITelegramBotClient bot,
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await bot.SendMessage(chatId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftDescription = text;
        session.State = TelegramBotState.WaitingForEventDate;

        await bot.SendMessage(
            chatId,
            "Шаг 3 из 4\nВведите дату (например: 14.06.2026):",
            cancellationToken: cancellationToken);
    }

    private async Task HandleWizardDateAsync(
        ITelegramBotClient bot,
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        if (!TryParseDate(text, out var date))
        {
            await bot.SendMessage(chatId, "Неверный формат даты. Пример: 14.06.2026", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftEventDate = date;
        session.State = TelegramBotState.WaitingForEventImage;

        await bot.SendMessage(chatId, "Шаг 4 из 4\nОтправьте изображение:", cancellationToken: cancellationToken);
    }

    private async Task CompleteAddWizardAsync(
        ITelegramBotClient bot,
        long chatId,
        Message message,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);

        if (string.IsNullOrWhiteSpace(session.DraftTitle) ||
            string.IsNullOrWhiteSpace(session.DraftDescription) ||
            session.DraftEventDate is null)
        {
            session.Reset();
            await bot.SendMessage(chatId, "⚠️ Данные мастера утеряны. Начните заново.", cancellationToken: cancellationToken);
            await StartAddWizardAsync(bot, chatId, cancellationToken);
            return;
        }

        var imagePath = await DownloadAndSavePhotoAsync(bot, message, cancellationToken);

        var entity = new Event
        {
            Title = session.DraftTitle,
            Description = session.DraftDescription,
            EventDate = session.DraftEventDate.Value,
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await events.CreateAsync(entity, cancellationToken);
        session.Reset();

        await bot.SendMessage(chatId, "✅ Мероприятие создано!", cancellationToken: cancellationToken);
        await SendEventsListAsync(bot, chatId, cancellationToken);
    }

    private async Task HandleEditTitleAsync(
        ITelegramBotClient bot,
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await bot.SendMessage(chatId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.EventId is null)
        {
            session.Reset();
            await SendMainMenuAsync(bot, chatId, cancellationToken);
            return;
        }

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
        {
            session.Reset();
            await bot.SendMessage(chatId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        entity.Title = text;
        await events.UpdateAsync(entity, cancellationToken);
        session.Reset();

        await bot.SendMessage(chatId, "✅ Название обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private async Task HandleEditDescriptionAsync(
        ITelegramBotClient bot,
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await bot.SendMessage(chatId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.EventId is null)
        {
            session.Reset();
            await SendMainMenuAsync(bot, chatId, cancellationToken);
            return;
        }

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
        {
            session.Reset();
            await bot.SendMessage(chatId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        entity.Description = text;
        await events.UpdateAsync(entity, cancellationToken);
        session.Reset();

        await bot.SendMessage(chatId, "✅ Описание обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private async Task CompleteEditImageAsync(
        ITelegramBotClient bot,
        long chatId,
        Message message,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        if (session.EventId is null)
        {
            session.Reset();
            await SendMainMenuAsync(bot, chatId, cancellationToken);
            return;
        }

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
        {
            session.Reset();
            await bot.SendMessage(chatId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        var oldPath = entity.ImagePath;
        var newPath = await DownloadAndSavePhotoAsync(bot, message, cancellationToken);

        entity.ImagePath = newPath;
        await events.UpdateAsync(entity, cancellationToken);
        await images.DeleteIfUploadedAsync(oldPath, cancellationToken);

        var eventId = entity.Id;
        session.Reset();

        await bot.SendMessage(chatId, "✅ Изображение обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(bot, chatId, eventId, cancellationToken);
    }

    private async Task<string> DownloadAndSavePhotoAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        var photo = message.Photo?.MaxBy(p => p.FileSize);
        if (photo is null)
            throw new InvalidOperationException("Изображение не найдено в сообщении.");

        var file = await bot.GetFile(photo.FileId, cancellationToken);
        if (string.IsNullOrEmpty(file.FilePath))
            throw new InvalidOperationException("Не удалось получить файл из Telegram.");

        await using var stream = new MemoryStream();
        await bot.DownloadFile(file.FilePath, stream, cancellationToken);
        stream.Position = 0;

        var extension = Path.GetExtension(file.FilePath);
        if (string.IsNullOrEmpty(extension))
            extension = ".jpg";

        return await images.SaveFromStreamAsync(stream, extension, cancellationToken);
    }

    private async Task<bool> EnsureEventExists(
        ITelegramBotClient bot,
        long chatId,
        int eventId,
        CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is not null)
            return true;

        await bot.SendMessage(chatId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
        await SendEventsListAsync(bot, chatId, cancellationToken);
        return false;
    }

    private static bool TryParseDate(string text, out DateTime date)
    {
        var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };
        return DateTime.TryParseExact(text, formats, RuCulture, DateTimeStyles.None, out date)
               || DateTime.TryParse(text, RuCulture, DateTimeStyles.None, out date);
    }

    private static InlineKeyboardMarkup BackToMainMenu() =>
        new([[InlineKeyboardButton.WithCallbackData("⬅ Главное меню", TelegramCallbackData.MenuMain)]]);
}
