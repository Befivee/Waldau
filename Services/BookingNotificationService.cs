using WaldauCastle.Models;
using WaldauCastle.Services.VK;

namespace WaldauCastle.Services;

public class BookingNotificationService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingNotificationService> logger) : IBookingNotificationService
{
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(20);

    public void ScheduleNewBookingNotification(Booking booking)
    {
        var snapshot = CloneBooking(booking);

        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(NotificationTimeout);
                await SendNotificationsAsync(snapshot, cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning(
                    "Уведомление о заявке #{BookingId} не успело отправиться за {TimeoutSeconds} с.",
                    snapshot.Id,
                    NotificationTimeout.TotalSeconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка фоновой отправки уведомления о заявке #{BookingId}.", snapshot.Id);
            }
        });
    }

    public Task NotifyNewBookingAsync(Booking booking, CancellationToken cancellationToken = default) =>
        SendNotificationsAsync(booking, cancellationToken);

    private async Task SendNotificationsAsync(Booking booking, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var telegram = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();
        var vk = scope.ServiceProvider.GetRequiredService<IVKNotificationService>();

        await Task.WhenAll(
            SafeNotifyAsync(() => telegram.NotifyNewBookingAsync(booking, cancellationToken), "Telegram", cancellationToken),
            SafeNotifyAsync(() => vk.NotifyNewBookingAsync(booking, cancellationToken), "VK", cancellationToken));
    }

    private async Task SafeNotifyAsync(
        Func<Task> send,
        string channel,
        CancellationToken cancellationToken)
    {
        try
        {
            await send();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Таймаут {Channel}-уведомления о заявке.", channel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка {Channel}-уведомления о заявке.", channel);
        }
    }

    private static Booking CloneBooking(Booking booking) =>
        new()
        {
            Id = booking.Id,
            FullName = booking.FullName,
            Phone = booking.Phone,
            VisitDate = booking.VisitDate,
            ExcursionKind = booking.ExcursionKind,
            ExcursionTitle = booking.ExcursionTitle,
            VisitTime = booking.VisitTime,
            PersonsCount = booking.PersonsCount,
            CreatedAt = booking.CreatedAt,
            PersonalDataConsent = booking.PersonalDataConsent
        };
}
