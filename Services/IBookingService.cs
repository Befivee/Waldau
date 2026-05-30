using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface IBookingService
{
    Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetLatestAsync(int count, CancellationToken cancellationToken = default);
}
