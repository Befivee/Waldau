using Microsoft.AspNetCore.Mvc;

namespace WaldauCastle.Controllers;

public class AboutController : Controller
{
    public IActionResult Index() => View();
}
