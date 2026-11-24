using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    public HomeController(ApiClient api) : base(api) { }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
        return View();
    }

    [Route("Home/Error")]
    public IActionResult Error() => View();
}
