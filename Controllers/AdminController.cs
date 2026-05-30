using Microsoft.AspNetCore.Mvc;
using WaldauCastle.Models;
using WaldauCastle.Services;
using WaldauCastle.ViewModels;

namespace WaldauCastle.Controllers;

public class AdminController(IEventService events, IEventImageService images) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var list = await events.GetAllAsync(cancellationToken);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() =>
        View(new EventEditViewModel { EventDate = DateTime.Today.AddMonths(1) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventEditViewModel model, CancellationToken cancellationToken)
    {
        if (!await TryApplyImageUploadAsync(model, null, cancellationToken))
            return View(model);

        if (!ModelState.IsValid)
            return View(model);

        var entity = model.ToEntity();
        entity.CreatedAt = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(entity.ImagePath))
            entity.ImagePath = Event.DefaultImagePath;

        await events.CreateAsync(entity, cancellationToken);
        TempData["AdminMessage"] = "Мероприятие создано.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return View(EventEditViewModel.FromEntity(entity));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventEditViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        var existing = await events.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        model.ImagePath = existing.ImagePath;

        if (!await TryApplyImageUploadAsync(model, existing.ImagePath, cancellationToken))
            return View(model);

        if (!ModelState.IsValid)
            return View(model);

        var entity = model.ToEntity();
        entity.CreatedAt = existing.CreatedAt;
        entity.UpdatedAt = existing.UpdatedAt;
        await events.UpdateAsync(entity, cancellationToken);
        TempData["AdminMessage"] = "Мероприятие обновлено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await events.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        return View(entity);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var existing = await events.GetByIdAsync(id, cancellationToken);
        if (existing is not null)
            await images.DeleteIfUploadedAsync(existing.ImagePath, cancellationToken);

        await events.DeleteAsync(id, cancellationToken);
        TempData["AdminMessage"] = "Мероприятие удалено.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TryApplyImageUploadAsync(
        EventEditViewModel model,
        string? previousImagePath,
        CancellationToken cancellationToken)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0)
            return true;

        try
        {
            var newPath = await images.SaveAsync(model.ImageFile, cancellationToken);
            if (!string.IsNullOrEmpty(previousImagePath) && previousImagePath != newPath)
                await images.DeleteIfUploadedAsync(previousImagePath, cancellationToken);

            model.ImagePath = newPath;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.ImageFile), ex.Message);
            return false;
        }
    }
}
