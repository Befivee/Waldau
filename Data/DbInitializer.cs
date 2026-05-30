using Microsoft.EntityFrameworkCore;
using WaldauCastle.Models;

namespace WaldauCastle.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Excursions.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        context.Excursions.AddRange(
            new Excursion
            {
                Title = "Обзорная экскурсия",
                Description = "Прогулка по руинам крепости, башням и внутреннему двору с рассказом об истории Вальдау.",
                Duration = "45 мин",
                Price = 350
            },
            new Excursion
            {
                Title = "Музей и экспозиция",
                Description = "Экскурсия по залам музея: Тевтонский орден, быт крестоносцев, археологические находки.",
                Duration = "60 мин",
                Price = 450
            },
            new Excursion
            {
                Title = "Семейная программа",
                Description = "Интерактив для детей: рыцарские доспехи, средневековые игры, мастер-класс по геральдике.",
                Duration = "75 мин",
                Price = 500
            },
            new Excursion
            {
                Title = "Вечерний замок",
                Description = "Атмосферная прогулка в сумерках с факелами и рассказами о легендах крепости.",
                Duration = "50 мин",
                Price = 600
            },
            new Excursion
            {
                Title = "Фотопрогулка",
                Description = "Лучшие ракурсы замка с гидом-фотографом. Идеально для путешественников и блогеров.",
                Duration = "40 мин",
                Price = 400
            },
            new Excursion
            {
                Title = "Групповой тур",
                Description = "Для школ и организаций от 15 человек. Индивидуальный график и расширенная программа.",
                Duration = "90 мин",
                Price = 300
            });

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
