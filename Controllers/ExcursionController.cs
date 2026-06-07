using Microsoft.AspNetCore.Mvc;
using WaldauCastle.Models;

namespace WaldauCastle.Controllers;

public class ExcursionController : Controller
{
    public IActionResult Index()
    {
        ViewData["MetaDescription"] =
            "Экскурсии по замку Вальдау: с экскурсоводом или самостоятельное посещение. Запись онлайн.";
        ViewData["MetaKeywords"] = "экскурсии замок Вальдау, с гидом, самостоятельное посещение, Низовье";
        ViewData["OgType"] = "website";

        return View(ExcursionCatalog.All);
    }
}
