using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface IBookingService
{
    Task<Booking> CreateAsync(Booking booking, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetLatestAsync(int count, CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredByVisitDateAsync(DateTime visitDateBefore, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetOccupiedGuidedSlotsAsync(DateTime visitDate, CancellationToken cancellationToken = default);

    Task<bool> IsGuidedSlotAvailableAsync(DateTime visitDate, string visitTime, CancellationToken cancellationToken = default);
}
