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
        await context.Bookings
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
}
