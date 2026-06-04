using WaldauCastle.Models;
using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services.VK;

public partial class VKAdminManager
{
    public async Task SendBookingsAsync(long peerId, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        session.State = VKBotState.None;
        session.Screen = BotScreen.Bookings;
        session.PendingDeleteBookingId = null;

        await SendBookingsPageAsync(peerId, session.ListPage, cancellationToken);
    }

    private async Task SendBookingsPageAsync(long peerId, int page, CancellationToken cancellationToken)
    {
        var session = stateService.GetOrCreate(peerId);
        var (text, totalPages) = await content.BuildBookingsPageAsync(page, cancellationToken);
        session.ListPage = Math.Clamp(page, 0, totalPages - 1);

        var list = await bookings.GetPageAsync(session.ListPage, BotListPaging.PageSize, cancellationToken);
        session.PageIds = list.Select(b => b.Id).ToList();

        await apiClient.SendMessageAsync(
            peerId,
            text,
            VKKeyboards.BookingsPage(list, session.ListPage, totalPages),
            cancellationToken);
    }

    public async Task SendBookingDeleteConfirmationAsync(long peerId, int bookingId, CancellationToken cancellationToken)
    {
        var entity = await bookings.GetByIdAsync(bookingId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Заявка не найдена.", cancellationToken: cancellationToken);
            await SendBookingsAsync(peerId, cancellationToken);
            return;
        }

        var session = stateService.GetOrCreate(peerId);
        session.PendingDeleteBookingId = bookingId;

        await apiClient.SendMessageAsync(
            peerId,
            $"🗑 Удалить заявку «{entity.FullName}» ({entity.VisitDate:d})?",
            VKKeyboards.DeleteConfirmation(),
            cancellationToken);
    }

    public async Task DeleteBookingAsync(long peerId, int bookingId, CancellationToken cancellationToken)
    {
        var entity = await bookings.GetByIdAsync(bookingId, cancellationToken);
        if (entity is null)
        {
            await apiClient.SendMessageAsync(peerId, "Заявка уже удалена.", cancellationToken: cancellationToken);
        }
        else
        {
            await bookings.DeleteAsync(bookingId, cancellationToken);
            await apiClient.SendMessageAsync(peerId, "✅ Заявка удалена.", cancellationToken: cancellationToken);
        }

        var session = stateService.GetOrCreate(peerId);
        session.PendingDeleteBookingId = null;
        await SendBookingsPageAsync(peerId, session.ListPage, cancellationToken);
    }
}
