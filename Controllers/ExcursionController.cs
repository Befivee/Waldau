using Microsoft.AspNetCore.Mvc;
using WaldauCastle.Services;

namespace WaldauCastle.Controllers;

public class ExcursionController(IExcursionService excursions) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["MetaDescription"] =
            "Экскурсии по замку Вальдау: обзорные прогулки, музей, семейные и вечерние программы. Запись онлайн.";
        ViewData["MetaKeywords"] = "экскурсии замок Вальдау, музей, туры, Низовье";
        ViewData["OgType"] = "website";

        var list = await excursions.GetAllAsync(cancellationToken);
        return View(list);
    }
}
