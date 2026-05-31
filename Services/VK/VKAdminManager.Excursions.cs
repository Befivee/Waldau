using WaldauCastle.Models;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.VK;

public partial class VKAdminManager
{
    public async Task SendExcursionsListAsync(long peerId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.None;
        session.Screen = BotScreen.Excursions;
        session.PendingDeleteBookingId = null;

        await SendExcursionsPageAsync(peerId, session.ListPage, cancellationToken);
    }

    private async Task SendExcursionsPageAsync(long peerId, int page, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        var all = await excursions.GetAllAsync(cancellationToken);
        var totalPages = BotListPaging.TotalPages(all.Count);
        session.ListPage = Math.Clamp(page, 0, totalPages - 1);

        var paged = BotListPaging.GetPage(all, session.ListPage);
        session.PageIds = paged.Select(e => e.Id).ToList();

        var intro = all.Count == 0
            ? "🚶 Экскурсий пока нет.\n\nНажмите «➕ Добавить»."
            : CastleAdminContentService.BuildNumberedExcursionsIntro(all, session.ListPage);

        await apiClient.SendMessageAsync(
            peerId,
            intro,
            VKKeyboards.ExcursionsPage(paged, session.ListPage, totalPages),
            cancellationToken);
    }

    public async Task SendExcursionDetailsAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        var entity = await excursions.GetByIdAsync(excursionId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Экскурсия не найдена.", cancellationToken: cancellationToken);
            await SendExcursionsListAsync(peerId, cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.None;
        session.Screen = BotScreen.ExcursionDetail;
        session.ExcursionId = excursionId;
        session.PageIds = [excursionId];

        await apiClient.SendMessageAsync(
            peerId,
            content.BuildExcursionDetailsText(entity) +
            "\n\n1. ✏ Название\n2. 📝 Описание\n3. ⏱ Длительность\n4. 💰 Цена\n5. 🖼 Изображение\n6. 🗑 Удалить",
            VKKeyboards.ExcursionManagement(excursionId),
            cancellationToken);
    }

    public async Task StartExcursionEditImageAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(peerId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewExcursionImage;
        session.ExcursionId = excursionId;

        await apiClient.SendMessageAsync(
            peerId,
            "🖼 Отправьте новое изображение или «-» для стандартного.",
            VKKeyboards.Remove(),
            cancellationToken);
    }

    public async Task StartExcursionAddWizardAsync(long peerId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForExcursionTitle;
        session.DraftTitle = null;
        session.DraftDescription = null;
        session.DraftDuration = null;
        session.DraftPrice = null;

        await apiClient.SendMessageAsync(
            peerId,
            "➕ Новая экскурсия\n\nШаг 1 из 5\nВведите название:",
            VKKeyboards.Remove(),
            cancellationToken);
    }

    public async Task StartExcursionEditTitleAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(peerId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewExcursionTitle;
        session.ExcursionId = excursionId;

        await apiClient.SendMessageAsync(peerId, "✏ Введите новое название:", VKKeyboards.Remove(), cancellationToken);
    }

    public async Task StartExcursionEditDescriptionAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(peerId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewExcursionDescription;
        session.ExcursionId = excursionId;

        await apiClient.SendMessageAsync(peerId, "📝 Введите новое описание:", VKKeyboards.Remove(), cancellationToken);
    }

    public async Task StartExcursionEditDurationAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(peerId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewExcursionDuration;
        session.ExcursionId = excursionId;

        await apiClient.SendMessageAsync(peerId, "⏱ Введите длительность (например: 45 мин):", VKKeyboards.Remove(), cancellationToken);
    }

    public async Task StartExcursionEditPriceAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        if (!await EnsureExcursionExists(peerId, excursionId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewExcursionPrice;
        session.ExcursionId = excursionId;

        await apiClient.SendMessageAsync(peerId, "💰 Введите цену (число):", VKKeyboards.Remove(), cancellationToken);
    }

    public async Task SendExcursionDeleteConfirmationAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        var entity = await excursions.GetByIdAsync(excursionId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Экскурсия не найдена.", cancellationToken: cancellationToken);
            return;
        }

        await apiClient.SendMessageAsync(
            peerId,
            $"🗑 Удалить экскурсию «{entity.Title}»?",
            VKKeyboards.DeleteConfirmation(
                BotCallbackData.ExcursionDeleteYes(excursionId),
                BotCallbackData.ExcursionDeleteNo(excursionId)),
            cancellationToken);

        stateService.GetOrCreate(peerId).PendingDeleteExcursionId = excursionId;
    }

    public async Task DeleteExcursionAsync(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        var entity = await excursions.GetByIdAsync(excursionId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Экскурсия уже удалена.", cancellationToken: cancellationToken);
        }
        else
        {
            await images.DeleteIfUploadedAsync(entity.ImagePath, cancellationToken);
            await excursions.DeleteAsync(excursionId, cancellationToken);
            await apiClient.SendMessageAsync(peerId, "✅ Экскурсия удалена.", cancellationToken: cancellationToken);
        }

        await SendExcursionsListAsync(peerId, cancellationToken);
    }

    private async Task<bool> EnsureExcursionExists(long peerId, int excursionId, CancellationToken cancellationToken)
    {
        if (await excursions.GetByIdAsync(excursionId, cancellationToken) is not null)
            return true;

        await apiClient.SendMessageAsync(peerId, "Экскурсия не найдена.", cancellationToken: cancellationToken);
        await SendExcursionsListAsync(peerId, cancellationToken);
        return false;
    }

    private async Task HandleExcursionWizardTitleAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await apiClient.SendMessageAsync(peerId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.DraftTitle = text;
        session.State = VKBotState.WaitingForExcursionDescription;

        await apiClient.SendMessageAsync(peerId, "Шаг 2 из 5\nВведите описание:", VKKeyboards.Remove(), cancellationToken);
    }

    private async Task HandleExcursionWizardDescriptionAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await apiClient.SendMessageAsync(peerId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.DraftDescription = text;
        session.State = VKBotState.WaitingForExcursionDuration;

        await apiClient.SendMessageAsync(peerId, "Шаг 3 из 5\nВведите длительность:", VKKeyboards.Remove(), cancellationToken);
    }

    private async Task HandleExcursionWizardDurationAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 50)
        {
            await apiClient.SendMessageAsync(peerId, "Длительность должна быть от 2 до 50 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.DraftDuration = text;
        session.State = VKBotState.WaitingForExcursionPrice;

        await apiClient.SendMessageAsync(peerId, "Шаг 4 из 5\nВведите цену:", VKKeyboards.Remove(), cancellationToken);
    }

    private async Task HandleExcursionWizardPriceAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (!TryParsePrice(text, out var price))
        {
            await apiClient.SendMessageAsync(peerId, "Введите корректную цену (например: 500).", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (string.IsNullOrWhiteSpace(session.DraftTitle) ||
            string.IsNullOrWhiteSpace(session.DraftDescription) ||
            string.IsNullOrWhiteSpace(session.DraftDuration))
        {
            session.Reset();
            await apiClient.SendMessageAsync(peerId, "⚠️ Данные мастера утеряны. Начните заново.", cancellationToken: cancellationToken);
            await StartExcursionAddWizardAsync(peerId, cancellationToken);
            return;
        }

        session.DraftPrice = price;
        session.State = VKBotState.WaitingForExcursionImage;

        await apiClient.SendMessageAsync(
            peerId,
            "Шаг 5 из 5\nОтправьте изображение или «-» для стандартного:",
            VKKeyboards.Remove(),
            cancellationToken);
    }

    public async Task HandleExcursionPhotoMessageAsync(long peerId, VkMessage message, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        var imagePath = await DownloadExcursionPhotoAsync(message, cancellationToken);

        if (session.State == VKBotState.WaitingForExcursionImage)
            await CompleteExcursionAddWizardWithPathAsync(peerId, imagePath, cancellationToken);
        else
            await CompleteExcursionEditImageWithPathAsync(peerId, imagePath, cancellationToken);
    }

    public async Task HandleExcursionImageFallbackAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text != "-")
        {
            await apiClient.SendMessageAsync(peerId, "🖼 Отправьте изображение или «-» для стандартного.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.State == VKBotState.WaitingForExcursionImage)
            await CompleteExcursionAddWizardWithPathAsync(peerId, DefaultImagePath, cancellationToken);
        else
            await CompleteExcursionEditImageWithPathAsync(peerId, DefaultImagePath, cancellationToken);
    }

    private async Task CompleteExcursionAddWizardWithPathAsync(long peerId, string imagePath, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);

        if (string.IsNullOrWhiteSpace(session.DraftTitle) ||
            string.IsNullOrWhiteSpace(session.DraftDescription) ||
            string.IsNullOrWhiteSpace(session.DraftDuration) ||
            session.DraftPrice is null)
        {
            session.Reset();
            await apiClient.SendMessageAsync(peerId, "⚠️ Данные мастера утеряны. Начните заново.", cancellationToken: cancellationToken);
            await StartExcursionAddWizardAsync(peerId, cancellationToken);
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
        await apiClient.SendMessageAsync(peerId, "✅ Экскурсия создана!", cancellationToken: cancellationToken);
        await SendExcursionsListAsync(peerId, cancellationToken);
    }

    private async Task CompleteExcursionEditImageWithPathAsync(long peerId, string imagePath, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
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
        session.State = VKBotState.None;

        await apiClient.SendMessageAsync(peerId, "✅ Изображение обновлено.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(peerId, excursionId, cancellationToken);
    }

    private async Task HandleExcursionEditTitleAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await apiClient.SendMessageAsync(peerId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Title = text;
        await excursions.UpdateAsync(entity, cancellationToken);
        var id = entity.Id;
        session.State = VKBotState.None;

        await apiClient.SendMessageAsync(peerId, "✅ Название обновлено.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(peerId, id, cancellationToken);
    }

    private async Task HandleExcursionEditDescriptionAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await apiClient.SendMessageAsync(peerId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Description = text;
        await excursions.UpdateAsync(entity, cancellationToken);
        var id = entity.Id;
        session.State = VKBotState.None;

        await apiClient.SendMessageAsync(peerId, "✅ Описание обновлено.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(peerId, id, cancellationToken);
    }

    private async Task HandleExcursionEditDurationAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 50)
        {
            await apiClient.SendMessageAsync(peerId, "Длительность должна быть от 2 до 50 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Duration = text;
        await excursions.UpdateAsync(entity, cancellationToken);
        var id = entity.Id;
        session.State = VKBotState.None;

        await apiClient.SendMessageAsync(peerId, "✅ Длительность обновлена.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(peerId, id, cancellationToken);
    }

    private async Task HandleExcursionEditPriceAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (!TryParsePrice(text, out var price))
        {
            await apiClient.SendMessageAsync(peerId, "Введите корректную цену.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.ExcursionId is null)
            return;

        var entity = await excursions.GetByIdAsync(session.ExcursionId.Value, cancellationToken);
        if (entity is null)
            return;

        entity.Price = price;
        await excursions.UpdateAsync(entity, cancellationToken);
        var id = entity.Id;
        session.State = VKBotState.None;

        await apiClient.SendMessageAsync(peerId, "✅ Цена обновлена.", cancellationToken: cancellationToken);
        await SendExcursionDetailsAsync(peerId, id, cancellationToken);
    }

    private static bool TryParsePrice(string text, out decimal price) =>
        decimal.TryParse(text.Replace(',', '.'), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out price) && price >= 0;
}
