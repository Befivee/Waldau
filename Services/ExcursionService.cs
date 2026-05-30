using Microsoft.EntityFrameworkCore;
using WaldauCastle.Data;
using WaldauCastle.Models;

namespace WaldauCastle.Services;

public class ExcursionService(ApplicationDbContext context) : IExcursionService
{
    public async Task<IReadOnlyList<Excursion>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Excursions
            .OrderBy(e => e.Price)
            .ToListAsync(cancellationToken);

    public async Task<Excursion?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Excursions.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Excursion>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default) =>
        await context.Excursions
            .OrderBy(e => e.Price)
            .Take(count)
            .ToListAsync(cancellationToken);
}
