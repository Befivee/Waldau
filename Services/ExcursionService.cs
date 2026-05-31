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

    public Task<Excursion?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Excursions.FindAsync([id], cancellationToken).AsTask();

    public async Task<IReadOnlyList<Excursion>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default) =>
        await context.Excursions
            .OrderBy(e => e.Price)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<Excursion> CreateAsync(Excursion entity, CancellationToken cancellationToken = default)
    {
        context.Excursions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Excursion entity, CancellationToken cancellationToken = default)
    {
        context.Excursions.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Excursions.FindAsync([id], cancellationToken);
        if (entity is null)
            return;

        context.Excursions.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
