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
}
