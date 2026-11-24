using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Shared;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected readonly ApiClient Api;
    protected AppController(ApiClient api) => Api = api;

    protected int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected void Success(string message) => TempData["Success"] = message;
    protected void Error(string message) => TempData["Error"] = message;

    protected IActionResult RedirectToDashboard() =>
        User.IsInRole(Roles.Organiser)
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Dashboard", "Participant");
}
