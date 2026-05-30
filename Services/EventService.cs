using Microsoft.EntityFrameworkCore;
using WaldauCastle.Data;
using WaldauCastle.Models;

namespace WaldauCastle.Services;

public class EventService(ApplicationDbContext context) : IEventService
{
    public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Events
            .OrderBy(e => e.EventDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Event>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await context.Events
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Events.FindAsync([id], cancellationToken);

    public async Task<Event> CreateAsync(Event entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        context.Events.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Event entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        context.Events.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Events.FindAsync([id], cancellationToken);
        if (entity is null)
            return;

        context.Events.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}
