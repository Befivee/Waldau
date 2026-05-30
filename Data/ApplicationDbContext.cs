using Microsoft.EntityFrameworkCore;
using WaldauCastle.Models;

namespace WaldauCastle.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Excursion> Excursions => Set<Excursion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Excursion>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.EventDate);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(b => b.VisitDate);
        });
    }
}
