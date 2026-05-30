using Microsoft.AspNetCore.Mvc;
using WaldauCastle.Services;

namespace WaldauCastle.Controllers;

public class EventController(IEventService events) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["MetaDescription"] =
            "Мероприятия замка Вальдау: фестивали, концерты и культурные события на территории крепости XIII века.";
        ViewData["MetaKeywords"] = "мероприятия замок Вальдау, фестивали, концерты, Низовье";
        ViewData["OgType"] = "website";

        var list = await events.GetAllAsync(cancellationToken);
        return View(list);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var evt = await events.GetByIdAsync(id, cancellationToken);
        if (evt is null)
            return NotFound();

        ViewData["MetaDescription"] = evt.Description.Length > 160
            ? evt.Description[..157] + "..."
            : evt.Description;
        ViewData["MetaKeywords"] = $"{evt.Title}, замок Вальдау, мероприятия";
        ViewData["OgType"] = "article";
        ViewData["OgImage"] = evt.DisplayImagePath;

        return View(evt);
    }
}
