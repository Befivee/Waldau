using Microsoft.AspNetCore.Mvc;
using WaldauCastle.Models;
using WaldauCastle.Services;
using WaldauCastle.ViewModels;

namespace WaldauCastle.Controllers;

public class BookingController(IBookingService bookings, ITelegramNotificationService telegram) : Controller
{
    [HttpGet]
    public IActionResult Create(int? excursionId, string? excursionTitle)
    {
        var model = new BookingCreateViewModel
        {
            ExcursionId = excursionId,
            ExcursionTitle = excursionTitle
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var booking = new Booking
        {
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            VisitDate = model.VisitDate!.Value.Date,
            PersonsCount = model.PersonsCount,
            PersonalDataConsent = model.PersonalDataConsent,
            CreatedAt = DateTime.UtcNow
        };

        await bookings.CreateAsync(booking, cancellationToken);
        await telegram.NotifyNewBookingAsync(booking, cancellationToken);

        return RedirectToAction(nameof(Success));
    }

    [HttpGet("/booking/success")]
    public IActionResult Success() => View();
}
