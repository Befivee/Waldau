using Microsoft.AspNetCore.Mvc;

namespace WaldauCastle.Controllers;

public class PrivacyController : Controller
{
    [HttpGet("/privacy")]
    public IActionResult LegacyPrivacy() => RedirectPermanent("/privacy-policy");

    [HttpGet("/privacy-policy")]
    public IActionResult PrivacyPolicy()
    {
        ViewData["Title"] = "Политика конфиденциальности";
        ViewData["MetaDescription"] =
            "Политика конфиденциальности сайта «Замок Вальдау»: cookie, веб-аналитика, технические данные и права пользователей.";
        ViewData["MetaKeywords"] = "политика конфиденциальности, cookie, замок Вальдау";
        ViewData["BodyClass"] = "page-privacy";
        return View();
    }

    [HttpGet("/personal-data-policy")]
    public IActionResult PersonalDataPolicy()
    {
        ViewData["Title"] = "Политика обработки персональных данных";
        ViewData["MetaDescription"] =
            "Политика в отношении обработки персональных данных музейного комплекса «Замок Вальдау» в соответствии с 152-ФЗ.";
        ViewData["MetaKeywords"] = "персональные данные, 152-ФЗ, замок Вальдау";
        ViewData["BodyClass"] = "page-privacy";
        return View();
    }
}
