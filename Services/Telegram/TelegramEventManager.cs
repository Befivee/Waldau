using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WaldauCastle.Models;
using WaldauCastle.Services;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.Telegram;

public partial class TelegramEventManager(
    IEventService events,
    IBookingService bookings,
    IEventImageService images,
    CastleAdminContentService content,
    TelegramStateService stateService,
    ILogger<TelegramEventManager> logger)
{
    private static readonly CultureInfo RuCulture = new("ru-RU");
    private const string DefaultImagePath = "/images/tour-placeholder.svg";

    public async Task SendMainMenuAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.Reset();
        session.Screen = BotScreen.Main;

        await bot.SendMessage(
            chatId,
            "🏰 Панель управления замком Вальдау\n\n" +
            "1. 📋 Заявки\n" +
            "2. 🎭 Мероприятия\n" +
            "3. 📊 Статистика",
            replyMarkup: TelegramKeyboards.MainMenu(),
            cancellationToken: cancellationToken);
    }

    public async Task SendStatisticsAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.None;
        session.Screen = BotScreen.Stats;

        var text = await content.BuildStatisticsTextAsync(cancellationToken);
        await bot.SendMessage(chatId, text, replyMarkup: TelegramKeyboards.BackToMainMenu(), cancellationToken: cancellationToken);
    }

    public async Task SendEventsListAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.None;
        session.Screen = BotScreen.Events;
        session.PendingDeleteBookingId = null;

        await SendEventsPageAsync(bot, chatId, session.ListPage, cancellationToken);
    }

    private async Task SendEventsPageAsync(
        ITelegramBotClient bot,
        long chatId,
        int page,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        var all = await events.GetAllAsync(cancellationToken);
        var totalPages = BotListPaging.TotalPages(all.Count);
        session.ListPage = Math.Clamp(page, 0, totalPages - 1);

        var paged = BotListPaging.GetPage(all, session.ListPage);
        session.PageIds = paged.Select(e => e.Id).ToList();

        var intro = all.Count == 0
            ? "🎭 Мероприятий пока нет.\n\nНажмите «➕ Добавить»."
            : CastleAdminContentService.BuildNumberedEventsIntro(all, session.ListPage);

        await bot.SendMessage(
            chatId,
            intro,
            replyMarkup: TelegramKeyboards.EventsPage(paged, session.ListPage, totalPages),
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

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.None;
        session.Screen = BotScreen.EventDetail;
        session.EventId = eventId;
        session.PageIds = [eventId];

        await bot.SendMessage(
            chatId,
            content.BuildEventDetailsText(entity) +
            "\n\n1. ✏ Изменить название\n2. 📝 Изменить описание\n3. 🖼 Изменить изображение\n4. 🗑 Удалить",
            replyMarkup: TelegramKeyboards.EventManagement(),
            cancellationToken: cancellationToken);
    }

    public async Task StartAddWizardAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForEventTitle;

        await bot.SendMessage(
            chatId,
            "➕ Новое мероприятие\n\nШаг 1 из 4\nВведите название:",
            replyMarkup: TelegramKeyboards.Remove(),
            cancellationToken: cancellationToken);
    }

    public async Task StartEditTitleAsync(ITelegramBotClient bot, long chatId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(bot, chatId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewTitle;
        session.EventId = eventId;

        await bot.SendMessage(chatId, "✏ Введите новое название:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    public async Task StartEditDescriptionAsync(ITelegramBotClient bot, long chatId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(bot, chatId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewDescription;
        session.EventId = eventId;

        await bot.SendMessage(chatId, "📝 Введите новое описание:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    public async Task StartEditImageAsync(ITelegramBotClient bot, long chatId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(bot, chatId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewImage;
        session.EventId = eventId;

        await bot.SendMessage(
            chatId,
            "🖼 Отправьте новое изображение или «-» для стандартного.",
            replyMarkup: TelegramKeyboards.Remove(),
            cancellationToken: cancellationToken);
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
            replyMarkup: TelegramKeyboards.DeleteConfirmation(),
            cancellationToken: cancellationToken);

        stateService.GetOrCreate(chatId).PendingDeleteEventId = eventId;
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

        if ((int)session.State > (int)TelegramBotState.WaitingForNewImage)
            session.State = TelegramBotState.None;

        if (session.State == TelegramBotState.None)
        {
            await HandleMenuTextAsync(bot, chatId, text, cancellationToken);
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
                    await HandleImageFallbackAsync(bot, chatId, text, cancellationToken);
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
            await bot.SendMessage(chatId, "⚠️ Не удалось сохранить изображение. Отправьте «-» для стандартного.", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleWizardTitleAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await bot.SendMessage(chatId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftTitle = text;
        session.State = TelegramBotState.WaitingForEventDescription;

        await bot.SendMessage(chatId, "Шаг 2 из 4\nВведите описание:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    private async Task HandleWizardDescriptionAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await bot.SendMessage(chatId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftDescription = text;
        session.State = TelegramBotState.WaitingForEventDate;

        await bot.SendMessage(chatId, "Шаг 3 из 4\nВведите дату (например: 14.06.2026):", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    private async Task HandleWizardDateAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (!TryParseDate(text, out var date))
        {
            await bot.SendMessage(chatId, "Неверный формат даты. Пример: 14.06.2026", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftEventDate = date;
        session.State = TelegramBotState.WaitingForEventImage;

        await bot.SendMessage(
            chatId,
            "Шаг 4 из 4\nОтправьте изображение или «-» для стандартного:",
            replyMarkup: TelegramKeyboards.Remove(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleImageFallbackAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text != "-")
        {
            await bot.SendMessage(chatId, "🖼 Отправьте изображение или «-» для стандартного.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.State == TelegramBotState.WaitingForEventImage)
            await CompleteAddWizardWithPathAsync(bot, chatId, DefaultImagePath, cancellationToken);
        else
            await CompleteEditImageWithPathAsync(bot, chatId, DefaultImagePath, cancellationToken);
    }

    private async Task CompleteAddWizardAsync(
        ITelegramBotClient bot,
        long chatId,
        Message message,
        CancellationToken cancellationToken)
    {
        var imagePath = await DownloadAndSavePhotoAsync(bot, message, cancellationToken);
        await CompleteAddWizardWithPathAsync(bot, chatId, imagePath, cancellationToken);
    }

    private async Task CompleteAddWizardWithPathAsync(
        ITelegramBotClient bot,
        long chatId,
        string imagePath,
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

        await events.CreateAsync(new Event
        {
            Title = session.DraftTitle,
            Description = session.DraftDescription,
            EventDate = session.DraftEventDate.Value,
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);

        session.Reset();
        await bot.SendMessage(chatId, "✅ Мероприятие создано!", cancellationToken: cancellationToken);
        await SendEventsListAsync(bot, chatId, cancellationToken);
    }

    private async Task HandleEditTitleAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await bot.SendMessage(chatId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.EventId is null)
            return;

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Title = text;
        await events.UpdateAsync(entity, cancellationToken);
        session.State = TelegramBotState.None;

        await bot.SendMessage(chatId, "✅ Название обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private async Task HandleEditDescriptionAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await bot.SendMessage(chatId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.EventId is null)
            return;

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Description = text;
        await events.UpdateAsync(entity, cancellationToken);
        session.State = TelegramBotState.None;

        await bot.SendMessage(chatId, "✅ Описание обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private async Task CompleteEditImageAsync(
        ITelegramBotClient bot,
        long chatId,
        Message message,
        CancellationToken cancellationToken)
    {
        var imagePath = await DownloadAndSavePhotoAsync(bot, message, cancellationToken);
        await CompleteEditImageWithPathAsync(bot, chatId, imagePath, cancellationToken);
    }

    private async Task CompleteEditImageWithPathAsync(
        ITelegramBotClient bot,
        long chatId,
        string imagePath,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        if (session.EventId is null)
            return;

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
            return;

        var oldPath = entity.ImagePath;
        entity.ImagePath = imagePath;
        await events.UpdateAsync(entity, cancellationToken);
        await images.DeleteIfUploadedAsync(oldPath, cancellationToken);

        var eventId = entity.Id;
        session.State = TelegramBotState.None;

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

        return await images.SaveFromStreamAsync(stream, extension, cancellationToken: cancellationToken);
    }

    private async Task<bool> EnsureEventExists(
        ITelegramBotClient bot,
        long chatId,
        int eventId,
        CancellationToken cancellationToken)
    {
        if (await events.GetByIdAsync(eventId, cancellationToken) is not null)
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
}
