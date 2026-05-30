using Microsoft.AspNetCore.Mvc;

namespace WaldauCastle.Controllers;

public class ContactsController : Controller
{
    public IActionResult Index() => View();
}
