using Telegram.Bot;
using Telegram.Bot.Types;
using WaldauCastle.Models;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.Telegram;

public partial class TelegramEventManager
{
    public async Task SendExcursionsListAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.None;
        session.Screen = BotScreen.Excursions;
        session.PendingDeleteBookingId = null;

        await SendExcursionsPageAsync(bot, chatId, session.ListPage, cancellationToken);
    }

    private async Task SendExcursionsPageAsync(
        ITelegramBotClient bot,
        long chatId,
        int page,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        var all = await excursions.GetAllAsync(cancellationToken);
        var totalPages = BotListPaging.TotalPages(all.Count);
        session.ListPage = Math.Clamp(page, 0, totalPages - 1);

        var paged = BotListPaging.GetPage(all, session.ListPage);
        session.PageIds = paged.Select(e => e.Id).ToList();

        var intro = all.Count == 0
            ? "🚶 Экскурсий пока нет.\n\nНажмите «➕ Добавить»."
            : CastleAdminContentService.BuildNumberedExcursionsIntro(all, session.ListPage);

        await bot.SendMessage(
            chatId,
            intro,
            replyMarkup: TelegramKeyboards.ExcursionsPage(paged, session.ListPage, totalPages),
            cancellationToken: cancellationToken);
    }

    public async Task SendExcursionDetailsAsync(
        ITelegramBotClient bot,
        long chatId,
        int excursionId,
        CancellationToken cancellationToken)
    {
        var entity = await excursions.GetByIdAsync(excursionId, cancellationToken);
        if (entity is null)
        {
            await bot.SendMessage(chatId, "Экскурсия не найдена.", cancellationToken: cancellationToken);
            await SendExcursionsListAsync(bot, chatId, cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.None;
        session.Screen = BotScreen.ExcursionDetail;
        session.ExcursionId = excursionId;
        session.PageIds = [excursionId];

        await bot.SendMessage(
            chatId,
            content.BuildExcursionDetailsText(entity) +
            "\n\n1. ✏ Название\n2. 📝 Описание\n3. ⏱ Длительность\n4. 💰 Цена\n5. 🖼 Изображение\n6. 🗑 Удалить",
            replyMarkup: TelegramKeyboards.ExcursionManagement(),
            cancellationToken: cancellationToken);
    }

    public async Task StartExcursionEditImageAsync(
        ITelegramBotClient bot,
        long chatId,
        int excursionId,
        CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(bot, chatId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewExcursionImage;
        session.ExcursionId = excursionId;

        await bot.SendMessage(
            chatId,
            "🖼 Отправьте новое изображение или «-» для стандартного.",
            replyMarkup: TelegramKeyboards.Remove(),
            cancellationToken: cancellationToken);
    }

    public async Task StartExcursionAddWizardAsync(ITelegramBotClient bot, long chatId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForExcursionTitle;
        session.DraftTitle = null;
        session.DraftDescription = null;
        session.DraftDuration = null;
        session.DraftPrice = null;

        await bot.SendMessage(
            chatId,
            "➕ Новая экскурсия\n\nШаг 1 из 5\nВведите название:",
            replyMarkup: TelegramKeyboards.Remove(),
            cancellationToken: cancellationToken);
    }

    public async Task StartExcursionEditTitleAsync(ITelegramBotClient bot, long chatId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(bot, chatId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewExcursionTitle;
        session.ExcursionId = excursionId;

        await bot.SendMessage(chatId, "✏ Введите новое название:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    public async Task StartExcursionEditDescriptionAsync(ITelegramBotClient bot, long chatId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(bot, chatId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewExcursionDescription;
        session.ExcursionId = excursionId;

        await bot.SendMessage(chatId, "📝 Введите новое описание:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    public async Task StartExcursionEditDurationAsync(ITelegramBotClient bot, long chatId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(bot, chatId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewExcursionDuration;
        session.ExcursionId = excursionId;

        await bot.SendMessage(chatId, "⏱ Введите длительность:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    public async Task StartExcursionEditPriceAsync(ITelegramBotClient bot, long chatId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(bot, chatId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(chatId);
        session.State = TelegramBotState.WaitingForNewExcursionPrice;
        session.ExcursionId = excursionId;

        await bot.SendMessage(chatId, "💰 Введите цену:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    public async Task SendExcursionDeleteConfirmationAsync(
        ITelegramBotClient bot,
        long chatId,
        int excursionId,
        CancellationToken cancellationToken)
    {
        var entity = await excursions.GetByIdAsync(excursionId, cancellationToken);
        if (entity is null)
        {
            await bot.SendMessage(chatId, "Экскурсия не найдена.", cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(
            chatId,
            $"🗑 Удалить экскурсию «{entity.Title}»?",
            replyMarkup: TelegramKeyboards.DeleteConfirmation(),
            cancellationToken: cancellationToken);

        stateService.GetOrCreate(chatId).PendingDeleteExcursionId = excursionId;
    }

    public async Task DeleteExcursionAsync(
        ITelegramBotClient bot,
        long chatId,
        int excursionId,
        CancellationToken cancellationToken)
    {
        var entity = await excursions.GetByIdAsync(excursionId, cancellationToken);
        if (entity is null)
            await bot.SendMessage(chatId, "Экскурсия уже удалена.", cancellationToken: cancellationToken);
        else
        {
            await images.DeleteIfUploadedAsync(entity.ImagePath, cancellationToken);
            await excursions.DeleteAsync(excursionId, cancellationToken);
            await bot.SendMessage(chatId, "✅ Экскурсия удалена.", cancellationToken: cancellationToken);
        }

        await SendExcursionsListAsync(bot, chatId, cancellationToken);
    }

    private async Task<bool> EnsureExcursionExists(
        ITelegramBotClient bot,
        long chatId,
        int excursionId,
        CancellationToken cancellationToken)
    {
        if (await excursions.GetByIdAsync(excursionId, cancellationToken) is not null)
            return true;

        await bot.SendMessage(chatId, "Экскурсия не найдена.", cancellationToken: cancellationToken);
        await SendExcursionsListAsync(bot, chatId, cancellationToken);
        return false;
    }

    private async Task HandleExcursionWizardTitleAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await bot.SendMessage(chatId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftTitle = text;
        session.State = TelegramBotState.WaitingForExcursionDescription;

        await bot.SendMessage(chatId, "Шаг 2 из 5\nВведите описание:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    private async Task HandleExcursionWizardDescriptionAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await bot.SendMessage(chatId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftDescription = text;
        session.State = TelegramBotState.WaitingForExcursionDuration;

        await bot.SendMessage(chatId, "Шаг 3 из 5\nВведите длительность:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    private async Task HandleExcursionWizardDurationAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 50)
        {
            await bot.SendMessage(chatId, "Длительность должна быть от 2 до 50 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        session.DraftDuration = text;
        session.State = TelegramBotState.WaitingForExcursionPrice;

        await bot.SendMessage(chatId, "Шаг 4 из 5\nВведите цену:", replyMarkup: TelegramKeyboards.Remove(), cancellationToken: cancellationToken);
    }

    private async Task HandleExcursionWizardPriceAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (!TryParsePrice(text, out var price))
        {
            await bot.SendMessage(chatId, "Введите корректную цену (например: 500).", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (string.IsNullOrWhiteSpace(session.DraftTitle) ||
            string.IsNullOrWhiteSpace(session.DraftDescription) ||
            string.IsNullOrWhiteSpace(session.DraftDuration))
        {
            session.Reset();
            await bot.SendMessage(chatId, "⚠️ Данные мастера утеряны. Начните заново.", cancellationToken: cancellationToken);
            await StartExcursionAddWizardAsync(bot, chatId, cancellationToken);
            return;
        }

        session.DraftPrice = price;
        session.State = TelegramBotState.WaitingForExcursionImage;

        await bot.SendMessage(
            chatId,
            "Шаг 5 из 5\nОтправьте изображение или «-» для стандартного:",
            replyMarkup: TelegramKeyboards.Remove(),
            cancellationToken: cancellationToken);
    }

    public async Task HandleExcursionPhotoMessageAsync(
        ITelegramBotClient bot,
        Message message,
        CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var session = stateService.GetOrCreate(chatId);
        var imagePath = await DownloadAndSaveExcursionPhotoAsync(bot, message, cancellationToken);

        if (session.State == TelegramBotState.WaitingForExcursionImage)
            await CompleteExcursionAddWizardWithPathAsync(bot, chatId, imagePath, cancellationToken);
        else
            await CompleteExcursionEditImageWithPathAsync(bot, chatId, imagePath, cancellationToken);
    }

    public async Task HandleExcursionImageFallbackAsync(
        ITelegramBotClient bot,
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        if (text != "-")
        {
            await bot.SendMessage(chatId, "🖼 Отправьте изображение или «-» для стандартного.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.State == TelegramBotState.WaitingForExcursionImage)
            await CompleteExcursionAddWizardWithPathAsync(bot, chatId, DefaultImagePath, cancellationToken);
        else
            await CompleteExcursionEditImageWithPathAsync(bot, chatId, DefaultImagePath, cancellationToken);
    }

    private async Task CompleteExcursionAddWizardWithPathAsync(
        ITelegramBotClient bot,
        long chatId,
        string imagePath,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);

        if (string.IsNullOrWhiteSpace(session.DraftTitle) ||
            string.IsNullOrWhiteSpace(session.DraftDescription) ||
            string.IsNullOrWhiteSpace(session.DraftDuration) ||
            session.DraftPrice is null)
        {
            session.Reset();
            await bot.SendMessage(chatId, "⚠️ Данные мастера утеряны. Начните заново.", cancellationToken: cancellationToken);
            await StartExcursionAddWizardAsync(bot, chatId, cancellationToken);
            return;
        }

        await excursions.CreateAsync(new Excursion
        {
            Title = session.DraftTitle,
            Description = session.DraftDescription,
            Duration = session.DraftDuration,
            Price = session.DraftPrice.Value,
            ImagePath = imagePath
        }, cancellationToken);

        session.Reset();
        await bot.SendMessage(chatId, "✅ Экскурсия создана!", cancellationToken: cancellationToken);
        await SendExcursionsListAsync(bot, chatId, cancellationToken);
    }

    private async Task CompleteExcursionEditImageWithPathAsync(
        ITelegramBotClient bot,
        long chatId,
        string imagePath,
        CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(chatId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        var oldPath = entity.ImagePath;
        entity.ImagePath = imagePath;
        await excursions.UpdateAsync(entity, cancellationToken);
        await images.DeleteIfUploadedAsync(oldPath, cancellationToken);

        var excursionId = entity.Id;
        session.State = TelegramBotState.None;

        await bot.SendMessage(chatId, "✅ Изображение обновлено.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(bot, chatId, excursionId, cancellationToken);
    }

    private async Task<string> DownloadAndSaveExcursionPhotoAsync(
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

        return await images.SaveFromStreamAsync(stream, extension, "excursions", cancellationToken: cancellationToken);
    }

    private async Task HandleExcursionEditTitleAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await bot.SendMessage(chatId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Title = text;
        await excursions.UpdateAsync(entity, cancellationToken);
        session.State = TelegramBotState.None;

        await bot.SendMessage(chatId, "✅ Название обновлено.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private async Task HandleExcursionEditDescriptionAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await bot.SendMessage(chatId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Description = text;
        await excursions.UpdateAsync(entity, cancellationToken);
        session.State = TelegramBotState.None;

        await bot.SendMessage(chatId, "✅ Описание обновлено.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private async Task HandleExcursionEditDurationAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 50)
        {
            await bot.SendMessage(chatId, "Длительность должна быть от 2 до 50 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Duration = text;
        await excursions.UpdateAsync(entity, cancellationToken);
        session.State = TelegramBotState.None;

        await bot.SendMessage(chatId, "✅ Длительность обновлена.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private async Task HandleExcursionEditPriceAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (!TryParsePrice(text, out var price))
        {
            await bot.SendMessage(chatId, "Введите корректную цену.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(chatId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Price = price;
        await excursions.UpdateAsync(entity, cancellationToken);
        session.State = TelegramBotState.None;

        await bot.SendMessage(chatId, "✅ Цена обновлена.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(bot, chatId, entity.Id, cancellationToken);
    }

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text.Replace(',', '.'), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out price) && price >= 0;
}
