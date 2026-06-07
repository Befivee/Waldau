using Microsoft.EntityFrameworkCore;
using WaldauCastle.Models;

namespace WaldauCastle.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Events.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        context.Events.AddRange(
            new Event
            {
                Title = "Рыцарский фестиваль",
                Description = "Турниры, реконструкция боёв, средневековая ярмарка на территории замка.",
                EventDate = new DateTime(2026, 6, 14),
                ImagePath = "/images/hero-castle.svg",
                CreatedAt = now
            },
            new Event
            {
                Title = "Ночь музеев",
                Description = "Специальная программа с экскурсиями, концертом и угощениями в таверне.",
                EventDate = new DateTime(2026, 5, 18),
                ImagePath = "/images/about-castle.svg",
                CreatedAt = now
            },
            new Event
            {
                Title = "Средневековый маркет",
                Description = "Ремесленники, мастер-классы, уличная еда и музыка минстrelей.",
                EventDate = new DateTime(2026, 7, 26),
                ImagePath = "/images/tour-placeholder.svg",
                CreatedAt = now
            },
            new Event
            {
                Title = "Концерт под звёздами",
                Description = "Камерная музыка во дворе крепости. Вход по предварительной записи.",
                EventDate = new DateTime(2026, 8, 9),
                ImagePath = "/images/about-castle.svg",
                CreatedAt = now
            });

        await context.SaveChangesAsync();
    }
}
