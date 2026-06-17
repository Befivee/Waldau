using WaldauCastle.Models;
using WaldauCastle.Services.VK;

namespace WaldauCastle.Services;

/// <summary>Фоновая обработка очереди уведомлений — независимо от polling Telegram-бота.</summary>
public class BookingNotificationWorker(
    BookingNotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BookingNotificationWorker запущен.");

        await foreach (var booking in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessBookingAsync(booking, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки уведомления о заявке #{BookingId}.", booking.Id);
            }
        }

        logger.LogInformation("BookingNotificationWorker остановлен.");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var vk = scope.ServiceProvider.GetRequiredService<IVKNotificationService>();
        var telegram = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        try
        {
            await vk.NotifyNewBookingAsync(booking, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VK-уведомление о заявке #{BookingId} не отправлено.", booking.Id);
        }

        await telegram.NotifyNewBookingAsync(booking, stoppingToken);
    }
}
