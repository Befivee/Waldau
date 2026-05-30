using Microsoft.AspNetCore.Mvc;

namespace WaldauCastle.Controllers;

public class PrivacyController : Controller
{
    [HttpGet("/privacy")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Политика конфиденциальности";
        ViewData["MetaDescription"] =
            "Политика обработки персональных данных музейного комплекса «Замок Вальдау». Цели сбора, хранение и права пользователей.";
        ViewData["MetaKeywords"] = "политика конфиденциальности, персональные данные, замок Вальдау";
        return View();
    }
}
