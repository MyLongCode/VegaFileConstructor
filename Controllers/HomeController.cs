using Microsoft.AspNetCore.Mvc;

namespace VegaFileConstructor.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
