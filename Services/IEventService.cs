using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface IEventService
{
    Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Event> CreateAsync(Event entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
