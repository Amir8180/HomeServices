using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Controllers;

public class PortfolioController : Controller
{
    public IActionResult Index() => View();
}
