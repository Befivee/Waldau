using Microsoft.EntityFrameworkCore;
using WaldauCastle.Data;
using WaldauCastle.Models;

namespace WaldauCastle.Services;

public class BookingService(ApplicationDbContext context) : IBookingService
{
    public async Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        booking.CreatedAt = DateTime.UtcNow;
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<IReadOnlyList<Booking>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        await QueryOrdered()
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default) =>
        context.Bookings.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 0)
            page = 0;

        return await QueryOrdered()
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Bookings.FindAsync([id], cancellationToken).AsTask();

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Bookings.FindAsync([id], cancellationToken);
        if (entity is null)
            return;

        context.Bookings.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredByVisitDateAsync(
        DateTime visitDateBefore,
        CancellationToken cancellationToken = default)
    {
        var expired = await context.Bookings
            .Where(b => b.VisitDate.Date < visitDateBefore.Date)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return 0;

        context.Bookings.RemoveRange(expired);
        await context.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    public async Task<IReadOnlyList<string>> GetOccupiedGuidedSlotsAsync(
        DateTime visitDate,
        CancellationToken cancellationToken = default) =>
        await context.Bookings
            .Where(b =>
                b.ExcursionKind == ExcursionKind.Guided &&
                b.VisitDate.Date == visitDate.Date &&
                b.VisitTime != null)
            .Select(b => b.VisitTime!)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<bool> IsGuidedSlotAvailableAsync(
        DateTime visitDate,
        string visitTime,
        CancellationToken cancellationToken = default)
    {
        var occupied = await GetOccupiedGuidedSlotsAsync(visitDate, cancellationToken);
        return !occupied.Contains(visitTime);
    }

    private static readonly TimeSpan PendingNotificationMaxAge = TimeSpan.FromHours(72);
    private static readonly TimeSpan NotificationRetryMinAge = TimeSpan.FromSeconds(10);

    public async Task<IReadOnlyList<Booking>> GetPendingAdminNotificationsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            limit = 20;

        var now = DateTime.UtcNow;
        var retryAfter = now.Subtract(NotificationRetryMinAge);
        var createdAfter = now.Subtract(PendingNotificationMaxAge);

        return await context.Bookings
            .Where(b =>
                b.CreatedAt >= createdAfter &&
                b.CreatedAt <= retryAfter &&
                (b.TelegramNotifiedAt == null || b.VkNotifiedAt == null))
            .OrderBy(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkTelegramNotifiedAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        var entity = await context.Bookings.FindAsync([bookingId], cancellationToken);
        if (entity is null)
            return;

        entity.TelegramNotifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkVkNotifiedAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        var entity = await context.Bookings.FindAsync([bookingId], cancellationToken);
        if (entity is null)
            return;

        entity.VkNotifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Booking> QueryOrdered() =>
        context.Bookings
            .OrderBy(b => b.VisitDate)
            .ThenByDescending(b => b.CreatedAt);
}
