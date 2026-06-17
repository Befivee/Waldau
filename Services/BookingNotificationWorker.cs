using WaldauCastle.Models;
using WaldauCastle.Services.VK;

namespace WaldauCastle.Services;

/// <summary>Фоновая отправка уведомлений: очередь + повтор из БД после перезапуска.</summary>
public class BookingNotificationWorker(
    BookingNotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim _concurrency = new(3, 3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BookingNotificationWorker запущен.");

        await Task.WhenAll(
            ProcessQueueAsync(stoppingToken),
            RetryPendingFromDatabaseAsync(stoppingToken));

        logger.LogInformation("BookingNotificationWorker остановлен.");
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        await foreach (var bookingId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            _ = DispatchNotificationAsync(bookingId, stoppingToken);
        }
    }

    private async Task RetryPendingFromDatabaseAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RetryInterval);

        await RetryPendingBatchAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RetryPendingBatchAsync(stoppingToken);
    }

    private async Task RetryPendingBatchAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var bookings = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var pending = await bookings.GetPendingAdminNotificationsAsync(limit: 20, stoppingToken);

            if (pending.Count == 0)
                return;

            logger.LogInformation("Повторная отправка уведомлений для {Count} заявок.", pending.Count);

            var tasks = pending
                .Select(booking => DispatchNotificationAsync(booking.Id, stoppingToken))
                .ToArray();

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка повторной отправки уведомлений из БД.");
        }
    }

    private async Task DispatchNotificationAsync(int bookingId, CancellationToken stoppingToken)
    {
        await _concurrency.WaitAsync(stoppingToken);
        try
        {
            await NotifyBookingAsync(bookingId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка обработки уведомления о заявке #{BookingId}.", bookingId);
        }
        finally
        {
            _concurrency.Release();
        }
    }

    private async Task NotifyBookingAsync(int bookingId, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var bookings = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var booking = await bookings.GetByIdAsync(bookingId, stoppingToken);

        if (booking is null)
        {
            logger.LogWarning("Заявка #{BookingId} не найдена для уведомления.", bookingId);
            return;
        }

        var vk = scope.ServiceProvider.GetRequiredService<IVKNotificationService>();
        var telegram = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        var tasks = new List<Task>();

        if (booking.VkNotifiedAt is null)
            tasks.Add(SendVkAsync(bookings, vk, booking, stoppingToken));

        if (booking.TelegramNotifiedAt is null)
            tasks.Add(SendTelegramAsync(bookings, telegram, booking, stoppingToken));

        if (tasks.Count == 0)
            return;

        await Task.WhenAll(tasks);
    }

    private async Task SendVkAsync(
        IBookingService bookings,
        IVKNotificationService vk,
        Booking booking,
        CancellationToken stoppingToken)
    {
        if (await vk.NotifyNewBookingAsync(booking, stoppingToken))
        {
            await bookings.MarkVkNotifiedAsync(booking.Id, stoppingToken);
            logger.LogInformation("VK-уведомление о заявке #{BookingId} подтверждено.", booking.Id);
        }
    }

    private async Task SendTelegramAsync(
        IBookingService bookings,
        ITelegramNotificationService telegram,
        Booking booking,
        CancellationToken stoppingToken)
    {
        if (await telegram.NotifyNewBookingAsync(booking, stoppingToken))
        {
            await bookings.MarkTelegramNotifiedAsync(booking.Id, stoppingToken);
            logger.LogInformation("Telegram-уведомление о заявке #{BookingId} подтверждено.", booking.Id);
        }
    }
}
