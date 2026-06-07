using Microsoft.AspNetCore.Mvc;
using WaldauCastle.Services;
using WaldauCastle.ViewModels;

namespace WaldauCastle.Controllers;

public class HomeController(IEventService events) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["MetaDescription"] =
            "Замок Вальдау — средневековая крепость XIII века в пос. Низовье. Экскурсии, мероприятия, музей, запись онлайн.";
        ViewData["MetaKeywords"] =
            "замок Вальдау, музей, экскурсии, мероприятия, Калининград, Низовье, средневековый замок";
        ViewData["OgType"] = "website";

        var model = new HomeIndexViewModel
        {
            UpcomingEvents = await events.GetUpcomingAsync(3, cancellationToken)
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult NotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Title"] = "Страница не найдена";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult ServerError()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        ViewData["Title"] = "Ошибка сервера";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodeError(int statusCode)
    {
        Response.StatusCode = statusCode;
        return statusCode switch
        {
            StatusCodes.Status404NotFound => View("NotFound"),
            >= 500 and < 600 => View("ServerError"),
            _ => View("Error")
        };
    }
}
