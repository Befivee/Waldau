using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WaldauCastle.Models;
using WaldauCastle.Services;
using WaldauCastle.ViewModels;

namespace WaldauCastle.Controllers;

public class BookingController(IBookingService bookings, IBookingNotificationService notifications) : Controller
{
    [HttpGet]
    public IActionResult Create(int? excursionId, string? excursionTitle)
    {
        if (excursionId is > 0)
            return RedirectToAction("Index", "Excursion");

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/booking/occupied-slots")]
    public async Task<IActionResult> OccupiedSlots(DateTime? date, CancellationToken cancellationToken)
    {
        if (date is null || date.Value.Date < DateTime.Today.AddDays(1))
            return Ok(Array.Empty<string>());

        var slots = await bookings.GetOccupiedGuidedSlotsAsync(date.Value.Date, cancellationToken);
        return Ok(slots);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model, CancellationToken cancellationToken)
    {
        NormalizePhone(model, ModelState);
        await ValidateExcursionAsync(model, ModelState, cancellationToken);

        if (!ModelState.IsValid)
        {
            if (IsModalRequest())
                return BadRequest(ValidationProblemDetails());

            return View(model);
        }

        var excursion = ExcursionCatalog.Get((ExcursionKind)model.ExcursionId!.Value);

        var booking = new Booking
        {
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            VisitDate = model.VisitDate!.Value.Date,
            ExcursionKind = excursion.Kind,
            ExcursionTitle = excursion.Title,
            VisitTime = excursion.RequiresTimeSlot ? model.VisitTime : null,
            PersonsCount = model.PersonsCount,
            PersonalDataConsent = model.PersonalDataConsent,
            CreatedAt = DateTime.UtcNow
        };

        await bookings.CreateAsync(booking, cancellationToken);
        await notifications.NotifyNewBookingAsync(booking, cancellationToken);

        if (IsModalRequest())
            return Ok(new { success = true });

        return RedirectToAction(nameof(Success));
    }

    [HttpGet("/booking/success")]
    public IActionResult Success() => View();

    private bool IsModalRequest() =>
        Request.Headers.TryGetValue("X-Booking-Modal", out var value) &&
        string.Equals(value.ToString(), "1", StringComparison.Ordinal);

    private object ValidationProblemDetails() =>
        new
        {
            errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray())
        };

    private async Task ValidateExcursionAsync(
        BookingCreateViewModel model,
        ModelStateDictionary modelState,
        CancellationToken cancellationToken)
    {
        if (!ExcursionCatalog.TryGetById(model.ExcursionId, out var excursion))
        {
            modelState.AddModelError(nameof(BookingCreateViewModel.ExcursionId), "Выберите вид экскурсии");
            return;
        }

        model.ExcursionTitle = excursion.Title;

        if (!excursion.RequiresTimeSlot)
        {
            model.VisitTime = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(model.VisitTime))
        {
            modelState.AddModelError(nameof(BookingCreateViewModel.VisitTime), "Выберите время визита");
            return;
        }

        if (!ExcursionCatalog.GuidedTimeSlots.Contains(model.VisitTime))
        {
            modelState.AddModelError(nameof(BookingCreateViewModel.VisitTime), "Выберите доступное время с 10:00 до 17:00");
            return;
        }

        if (model.VisitDate is null)
            return;

        if (!await bookings.IsGuidedSlotAvailableAsync(model.VisitDate.Value.Date, model.VisitTime, cancellationToken))
            modelState.AddModelError(nameof(BookingCreateViewModel.VisitTime), "Это время уже занято. Выберите другое.");
    }

    private static void NormalizePhone(BookingCreateViewModel model, ModelStateDictionary modelState)
    {
        var raw = model.Phone?.Trim() ?? string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith('7') || digits.StartsWith('8'))
            digits = digits[1..];

        if (digits.Length == 10)
        {
            model.Phone = "+7" + digits;
            return;
        }

        if (string.IsNullOrWhiteSpace(raw))
            return;

        modelState.AddModelError(
            nameof(BookingCreateViewModel.Phone),
            "Введите 10 цифр номера после +7");
    }
}
