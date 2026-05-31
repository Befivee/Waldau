using System.Globalization;
using System.Text.Json;
using WaldauCastle.Data;
using WaldauCastle.Models;
using WaldauCastle.Services;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.VK;

public partial class VKAdminManager(
    IEventService events,
    IExcursionService excursions,
    IBookingService bookings,
    IEventImageService images,
    CastleAdminContentService content,
    VKApiClient apiClient,
    VKStateService stateService,
    IHttpClientFactory httpClientFactory,
    ILogger<VKAdminManager> logger)
{
    private static readonly CultureInfo RuCulture = new("ru-RU");
    private const string DefaultImagePath = "/images/tour-placeholder.svg";

    public async Task SendMainMenuAsync(long peerId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.Reset();
        session.Screen = BotScreen.Main;

        await apiClient.SendMessageAsync(
            peerId,
            "🏰 Панель управления замком Вальдау\n\n" +
            "1. 📋 Заявки\n" +
            "2. 🎭 Ивенты\n" +
            "3. 🚶 Экскурсии\n" +
            "4. 📊 Статистика",
            VKKeyboards.MainMenu(),
            cancellationToken);
    }

    public async Task SendStatisticsAsync(long peerId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.None;
        session.Screen = BotScreen.Stats;

        var text = await content.BuildStatisticsTextAsync(cancellationToken);
        await apiClient.SendMessageAsync(peerId, text, VKKeyboards.BackToMainMenu(), cancellationToken);
    }

    public async Task SendEventsListAsync(long peerId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.None;
        session.Screen = BotScreen.Events;
        session.PendingDeleteBookingId = null;

        await SendEventsPageAsync(peerId, session.ListPage, cancellationToken);
    }

    private async Task SendEventsPageAsync(long peerId, int page, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        var all = await events.GetAllAsync(cancellationToken);
        var totalPages = BotListPaging.TotalPages(all.Count);
        session.ListPage = Math.Clamp(page, 0, totalPages - 1);

        var paged = BotListPaging.GetPage(all, session.ListPage);
        session.PageIds = paged.Select(e => e.Id).ToList();

        var intro = all.Count == 0
            ? "🎭 Мероприятий пока нет.\n\nНажмите «➕ Добавить»."
            : CastleAdminContentService.BuildNumberedEventsIntro(all, session.ListPage);

        await apiClient.SendMessageAsync(
            peerId,
            intro,
            VKKeyboards.EventsPage(paged, session.ListPage, totalPages),
            cancellationToken);
    }

    public async Task SendEventDetailsAsync(long peerId, int eventId, CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            await SendEventsListAsync(peerId, cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.None;
        session.Screen = BotScreen.EventDetail;
        session.EventId = eventId;
        session.PageIds = [eventId];

        await apiClient.SendMessageAsync(
            peerId,
            content.BuildEventDetailsText(entity) +
            "\n\n1. ✏ Изменить название\n" +
            "2. 📝 Изменить описание\n" +
            "3. 🖼 Изменить изображение\n" +
            "4. 🗑 Удалить",
            VKKeyboards.EventManagement(eventId),
            cancellationToken);
    }

    public async Task StartAddWizardAsync(long peerId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.Reset();
        session.State = VKBotState.WaitingForEventTitle;

        await apiClient.SendMessageAsync(
            peerId,
            "➕ Новое мероприятие\n\nШаг 1 из 4\nВведите название:",
            VKKeyboards.Remove(),
            cancellationToken);
    }

    public async Task StartEditTitleAsync(long peerId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(peerId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewTitle;
        session.EventId = eventId;

        await apiClient.SendMessageAsync(peerId, "✏ Введите новое название:", VKKeyboards.Remove(), cancellationToken);
    }

    public async Task StartEditDescriptionAsync(long peerId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(peerId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewDescription;
        session.EventId = eventId;

        await apiClient.SendMessageAsync(peerId, "📝 Введите новое описание:", VKKeyboards.Remove(), cancellationToken);
    }

    public async Task StartEditImageAsync(long peerId, int eventId, CancellationToken cancellationToken)
    {
        if (!await EnsureEventExists(peerId, eventId, cancellationToken))
            return;

        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.WaitingForNewImage;
        session.EventId = eventId;

        await apiClient.SendMessageAsync(
            peerId,
            "🖼 Отправьте новое изображение или «-» для стандартного.",
            VKKeyboards.Remove(),
            cancellationToken);
    }

    public async Task SendDeleteConfirmationAsync(long peerId, int eventId, CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        await apiClient.SendMessageAsync(
            peerId,
            $"🗑 Удалить мероприятие «{entity.Title}»?",
            VKKeyboards.DeleteConfirmation(
                BotCallbackData.EventDeleteYes(eventId),
                BotCallbackData.EventDeleteNo(eventId)),
            cancellationToken);

        stateService.GetOrCreate(peerId).PendingDeleteEventId = eventId;
    }

    public async Task DeleteEventAsync(long peerId, int eventId, CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Мероприятие уже удалено.", cancellationToken: cancellationToken);
            await SendEventsListAsync(peerId, cancellationToken);
            return;
        }

        await images.DeleteIfUploadedAsync(entity.ImagePath, cancellationToken);
        await events.DeleteAsync(eventId, cancellationToken);
        stateService.GetOrCreate(peerId).Reset();

        await apiClient.SendMessageAsync(peerId, "✅ Мероприятие удалено.", cancellationToken: cancellationToken);
        await SendEventsListAsync(peerId, cancellationToken);
    }

    public async Task HandleCallbackAsync(long peerId, string payload, CancellationToken cancellationToken)
    {
        await (payload switch
        {
            BotCallbackData.MenuMain => SendMainMenuAsync(peerId, cancellationToken),
            BotCallbackData.MenuBookings => SendBookingsAsync(peerId, cancellationToken),
            BotCallbackData.MenuEvents => SendEventsListWithResetAsync(peerId, cancellationToken),
            BotCallbackData.EventBackList => SendEventsListAsync(peerId, cancellationToken),
            BotCallbackData.MenuExcursions => SendExcursionsListWithResetAsync(peerId, cancellationToken),
            BotCallbackData.ExcursionBackList => SendExcursionsListAsync(peerId, cancellationToken),
            BotCallbackData.MenuStats => SendStatisticsAsync(peerId, cancellationToken),
            BotCallbackData.PagePrev => ChangeListPageAsync(peerId, -1, cancellationToken),
            BotCallbackData.PageNext => ChangeListPageAsync(peerId, 1, cancellationToken),
            BotCallbackData.EventAdd => StartAddWizardAsync(peerId, cancellationToken),
            BotCallbackData.ExcursionAdd => StartExcursionAddWizardAsync(peerId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:view:", out var viewId) =>
                SendEventDetailsAsync(peerId, viewId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:edit_title:", out var titleId) =>
                StartEditTitleAsync(peerId, titleId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:edit_desc:", out var descId) =>
                StartEditDescriptionAsync(peerId, descId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:edit_img:", out var imgId) =>
                StartEditImageAsync(peerId, imgId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:del:", out var delId) =>
                SendDeleteConfirmationAsync(peerId, delId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:del_yes:", out var delYesId) =>
                DeleteEventAsync(peerId, delYesId, cancellationToken),
            _ when BotCallbackData.TryParseEventId(payload, "evt:del_no:", out var delNoId) =>
                SendEventDetailsAsync(peerId, delNoId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:view:", out var excViewId) =>
                SendExcursionDetailsAsync(peerId, excViewId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:edit_title:", out var excTitleId) =>
                StartExcursionEditTitleAsync(peerId, excTitleId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:edit_desc:", out var excDescId) =>
                StartExcursionEditDescriptionAsync(peerId, excDescId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:edit_dur:", out var excDurId) =>
                StartExcursionEditDurationAsync(peerId, excDurId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:edit_price:", out var excPriceId) =>
                StartExcursionEditPriceAsync(peerId, excPriceId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:edit_img:", out var excImgId) =>
                StartExcursionEditImageAsync(peerId, excImgId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:del:", out var excDelId) =>
                SendExcursionDeleteConfirmationAsync(peerId, excDelId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:del_yes:", out var excDelYesId) =>
                DeleteExcursionAsync(peerId, excDelYesId, cancellationToken),
            _ when BotCallbackData.TryParseExcursionId(payload, "exc:del_no:", out var excDelNoId) =>
                SendExcursionDetailsAsync(peerId, excDelNoId, cancellationToken),
            _ when BotCallbackData.TryParseBookingId(payload, "book:del:", out var bookDelId) =>
                SendBookingDeleteConfirmationAsync(peerId, bookDelId, cancellationToken),
            _ when BotCallbackData.TryParseBookingId(payload, "book:del_yes:", out var bookDelYesId) =>
                DeleteBookingAsync(peerId, bookDelYesId, cancellationToken),
            _ when BotCallbackData.TryParseBookingId(payload, "book:del_no:", out _) =>
                SendBookingsAsync(peerId, cancellationToken),
            _ => apiClient.SendMessageAsync(peerId, "Неизвестная команда. /start", cancellationToken: cancellationToken)
        });
    }

    private async Task SendEventsListWithResetAsync(long peerId, CancellationToken cancellationToken)
    {
        stateService.GetOrCreate(peerId).ListPage = 0;
        await SendEventsListAsync(peerId, cancellationToken);
    }

    private async Task SendExcursionsListWithResetAsync(long peerId, CancellationToken cancellationToken)
    {
        stateService.GetOrCreate(peerId).ListPage = 0;
        await SendExcursionsListAsync(peerId, cancellationToken);
    }

    private async Task ChangeListPageAsync(long peerId, int delta, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.ListPage = Math.Max(0, session.ListPage + delta);

        switch (session.Screen)
        {
            case BotScreen.Bookings:
                await SendBookingsPageAsync(peerId, session.ListPage, cancellationToken);
                break;
            case BotScreen.Events:
                await SendEventsPageAsync(peerId, session.ListPage, cancellationToken);
                break;
            case BotScreen.Excursions:
                await SendExcursionsPageAsync(peerId, session.ListPage, cancellationToken);
                break;
        }
    }

    public async Task HandleMenuTextAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);

        if (BotTextCommandResolver.TryResolveConfirmation(text, out var confirmed))
        {
            if (session.PendingDeleteBookingId is int bookingId)
            {
                if (confirmed)
                    await DeleteBookingAsync(peerId, bookingId, cancellationToken);
                else
                {
                    session.PendingDeleteBookingId = null;
                    await SendBookingsAsync(peerId, cancellationToken);
                }
                return;
            }

            if (session.PendingDeleteEventId is int eventId)
            {
                if (confirmed)
                    await DeleteEventAsync(peerId, eventId, cancellationToken);
                else
                {
                    session.PendingDeleteEventId = null;
                    await SendEventDetailsAsync(peerId, eventId, cancellationToken);
                }
                return;
            }

            if (session.PendingDeleteExcursionId is int excursionId)
            {
                if (confirmed)
                    await DeleteExcursionAsync(peerId, excursionId, cancellationToken);
                else
                {
                    session.PendingDeleteExcursionId = null;
                    await SendExcursionDetailsAsync(peerId, excursionId, cancellationToken);
                }
                return;
            }
        }

        if (BotTextCommandResolver.TryResolve(text, session.Screen, session.PageIds, out var payload))
        {
            await HandleCallbackAsync(peerId, payload, cancellationToken);
            return;
        }

        await apiClient.SendMessageAsync(
            peerId,
            "Используйте /start для открытия панели управления.",
            cancellationToken: cancellationToken);
    }

    public async Task HandleTextMessageAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);

        if (session.State == VKBotState.None)
        {
            await HandleMenuTextAsync(peerId, text, cancellationToken);
            return;
        }

        try
        {
            switch (session.State)
            {
                case VKBotState.WaitingForEventTitle:
                    await HandleWizardTitleAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForEventDescription:
                    await HandleWizardDescriptionAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForEventDate:
                    await HandleWizardDateAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForNewTitle:
                    await HandleEditTitleAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForNewDescription:
                    await HandleEditDescriptionAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForEventImage:
                case VKBotState.WaitingForNewImage:
                    await HandleImageFallbackAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForExcursionImage:
                case VKBotState.WaitingForNewExcursionImage:
                    await HandleExcursionImageFallbackAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForExcursionTitle:
                    await HandleExcursionWizardTitleAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForExcursionDescription:
                    await HandleExcursionWizardDescriptionAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForExcursionDuration:
                    await HandleExcursionWizardDurationAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForExcursionPrice:
                    await HandleExcursionWizardPriceAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForNewExcursionTitle:
                    await HandleExcursionEditTitleAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForNewExcursionDescription:
                    await HandleExcursionEditDescriptionAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForNewExcursionDuration:
                    await HandleExcursionEditDurationAsync(peerId, text, cancellationToken);
                    break;
                case VKBotState.WaitingForNewExcursionPrice:
                    await HandleExcursionEditPriceAsync(peerId, text, cancellationToken);
                    break;
                default:
                    session.Reset();
                    await SendMainMenuAsync(peerId, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обработки текста VK для peer {PeerId}", peerId);
            await apiClient.SendMessageAsync(
                peerId,
                "⚠️ Не удалось обработать сообщение. Попробуйте снова или /start.",
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandlePhotoMessageAsync(long peerId, VkMessage message, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);

        if (session.State is not (VKBotState.WaitingForEventImage or VKBotState.WaitingForNewImage
            or VKBotState.WaitingForExcursionImage or VKBotState.WaitingForNewExcursionImage))
        {
            await apiClient.SendMessageAsync(peerId, "Сейчас изображение не ожидается. Используйте /start.", cancellationToken: cancellationToken);
            return;
        }

        try
        {
            if (session.State is VKBotState.WaitingForEventImage or VKBotState.WaitingForNewImage)
            {
                var imagePath = await DownloadPhotoAsync(message, cancellationToken);

                if (session.State == VKBotState.WaitingForEventImage)
                    await CompleteAddWizardAsync(peerId, imagePath, cancellationToken);
                else
                    await CompleteEditImageAsync(peerId, imagePath, cancellationToken);
            }
            else
            {
                await HandleExcursionPhotoMessageAsync(peerId, message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка загрузки изображения VK для peer {PeerId}", peerId);
            await apiClient.SendMessageAsync(
                peerId,
                "⚠️ Не удалось сохранить изображение. Отправьте «-» для стандартного.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleWizardTitleAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await apiClient.SendMessageAsync(peerId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.DraftTitle = text;
        session.State = VKBotState.WaitingForEventDescription;

        await apiClient.SendMessageAsync(peerId, "Шаг 2 из 4\nВведите описание:", VKKeyboards.Remove(), cancellationToken);
    }

    private async Task HandleWizardDescriptionAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await apiClient.SendMessageAsync(peerId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.DraftDescription = text;
        session.State = VKBotState.WaitingForEventDate;

        await apiClient.SendMessageAsync(
            peerId,
            "Шаг 3 из 4\nВведите дату (например: 14.06.2026):",
            VKKeyboards.Remove(),
            cancellationToken);
    }

    private async Task HandleWizardDateAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (!TryParseDate(text, out var date))
        {
            await apiClient.SendMessageAsync(peerId, "Неверный формат даты. Пример: 14.06.2026", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.DraftEventDate = date;
        session.State = VKBotState.WaitingForEventImage;

        await apiClient.SendMessageAsync(
            peerId,
            "Шаг 4 из 4\nОтправьте изображение или «-» для стандартного:",
            VKKeyboards.Remove(),
            cancellationToken);
    }

    private async Task HandleImageFallbackAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text != "-")
        {
            await apiClient.SendMessageAsync(peerId, "🖼 Отправьте изображение или «-» для стандартного.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.State == VKBotState.WaitingForEventImage)
            await CompleteAddWizardAsync(peerId, DefaultImagePath, cancellationToken);
        else if (session.State == VKBotState.WaitingForNewImage)
            await CompleteEditImageAsync(peerId, DefaultImagePath, cancellationToken);
        else if (session.State == VKBotState.WaitingForExcursionImage)
            await CompleteExcursionAddWizardWithPathAsync(peerId, DefaultImagePath, cancellationToken);
        else
            await CompleteExcursionEditImageWithPathAsync(peerId, DefaultImagePath, cancellationToken);
    }

    private async Task CompleteAddWizardAsync(long peerId, string imagePath, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);

        if (string.IsNullOrWhiteSpace(session.DraftTitle) ||
            string.IsNullOrWhiteSpace(session.DraftDescription) ||
            session.DraftEventDate is null)
        {
            session.Reset();
            await apiClient.SendMessageAsync(peerId, "⚠️ Данные мастера утеряны. Начните заново.", cancellationToken: cancellationToken);
            await StartAddWizardAsync(peerId, cancellationToken);
            return;
        }

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

        await apiClient.SendMessageAsync(peerId, "✅ Мероприятие создано!", cancellationToken: cancellationToken);
        await SendEventsListAsync(peerId, cancellationToken);
    }

    private async Task HandleEditTitleAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 2 || text.Length > 200)
        {
            await apiClient.SendMessageAsync(peerId, "Название должно быть от 2 до 200 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.EventId is null)
        {
            session.Reset();
            await SendMainMenuAsync(peerId, cancellationToken);
            return;
        }

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
        {
            session.Reset();
            await apiClient.SendMessageAsync(peerId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        entity.Title = text;
        await events.UpdateAsync(entity, cancellationToken);
        var eventId = entity.Id;
        session.Reset();

        await apiClient.SendMessageAsync(peerId, "✅ Название обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(peerId, eventId, cancellationToken);
    }

    private async Task HandleEditDescriptionAsync(long peerId, string text, CancellationToken cancellationToken)
    {
        if (text.Length < 10 || text.Length > 2000)
        {
            await apiClient.SendMessageAsync(peerId, "Описание должно быть от 10 до 2000 символов.", cancellationToken: cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        if (session.EventId is null)
        {
            session.Reset();
            await SendMainMenuAsync(peerId, cancellationToken);
            return;
        }

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
        {
            session.Reset();
            await apiClient.SendMessageAsync(peerId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        entity.Description = text;
        await events.UpdateAsync(entity, cancellationToken);
        var eventId = entity.Id;
        session.Reset();

        await apiClient.SendMessageAsync(peerId, "✅ Описание обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(peerId, eventId, cancellationToken);
    }

    private async Task CompleteEditImageAsync(long peerId, string imagePath, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        if (session.EventId is null)
        {
            session.Reset();
            await SendMainMenuAsync(peerId, cancellationToken);
            return;
        }

        var entity = await events.GetByIdAsync(session.EventId.Value, cancellationToken);
        if (entity is null)
        {
            session.Reset();
            await apiClient.SendMessageAsync(peerId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
            return;
        }

        var oldPath = entity.ImagePath;
        entity.ImagePath = imagePath;
        await events.UpdateAsync(entity, cancellationToken);
        await images.DeleteIfUploadedAsync(oldPath, cancellationToken);

        var eventId = entity.Id;
        session.Reset();

        await apiClient.SendMessageAsync(peerId, "✅ Изображение обновлено.", cancellationToken: cancellationToken);
        await SendEventDetailsAsync(peerId, eventId, cancellationToken);
    }

    private async Task<string> DownloadPhotoAsync(VkMessage message, CancellationToken cancellationToken)
    {
        var url = message.GetLargestPhotoUrl();
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Изображение не найдено в сообщении.");

        using var client = httpClientFactory.CreateClient("vk_photo_download");
        await using var stream = await client.GetStreamAsync(url, cancellationToken);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        return await images.SaveFromStreamAsync(memory, ".jpg", cancellationToken: cancellationToken);
    }

    private async Task<string> DownloadExcursionPhotoAsync(VkMessage message, CancellationToken cancellationToken)
    {
        var url = message.GetLargestPhotoUrl();
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Изображение не найдено в сообщении.");

        using var client = httpClientFactory.CreateClient("vk_photo_download");
        await using var stream = await client.GetStreamAsync(url, cancellationToken);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        return await images.SaveFromStreamAsync(memory, ".jpg", "excursions", cancellationToken: cancellationToken);
    }

    private async Task<bool> EnsureEventExists(long peerId, int eventId, CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(eventId, cancellationToken);
        if (entity is not null)
            return true;

        await apiClient.SendMessageAsync(peerId, "Мероприятие не найдено.", cancellationToken: cancellationToken);
        await SendEventsListAsync(peerId, cancellationToken);
        return false;
    }

    private static bool TryParseDate(string text, out DateTime date)
    {
        var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };
        return DateTime.TryParseExact(text, formats, RuCulture, DateTimeStyles.None, out date)
               || DateTime.TryParse(text, RuCulture, DateTimeStyles.None, out date);
    }
}
