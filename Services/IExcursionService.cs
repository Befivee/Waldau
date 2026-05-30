using WaldauCastle.Models;

namespace WaldauCastle.Services;

public interface IExcursionService
{
    Task<IReadOnlyList<Excursion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Excursion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Excursion>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
}
