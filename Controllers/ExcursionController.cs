using Microsoft.AspNetCore.Mvc;
using WaldauCastle.Models;

namespace WaldauCastle.Controllers;

public class ExcursionController : Controller
{
    public IActionResult Index()
    {
        ViewData["MetaDescription"] =
            "Посещение замка Вальдау с аудиогидом. Запись онлайн.";
        ViewData["MetaKeywords"] = "экскурсии замок Вальдау, посещение с аудиогидом, аудиогид, Низовье";
        ViewData["OgType"] = "website";

        return View(ExcursionCatalog.WebsiteOfferings);
    }
}
