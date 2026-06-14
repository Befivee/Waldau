using WaldauCastle.Services.Bot;

namespace WaldauCastle.Services;

public class BookingCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        await RunCleanupAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCleanupAsync(stoppingToken);
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var bookings = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var deleted = await bookings.DeleteExpiredByVisitDateAsync(BotTime.LocalToday, cancellationToken);

            if (deleted > 0)
            {
                logger.LogInformation(
                    "Удалено {Count} заявок с датой визита до {Cutoff:yyyy-MM-dd} (Калининград).",
                    deleted,
                    BotTime.LocalToday);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка автоматического удаления заявок.");
        }
    }
}
