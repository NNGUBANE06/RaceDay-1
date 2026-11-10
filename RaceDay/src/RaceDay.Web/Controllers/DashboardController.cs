using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class DashboardController : Controller
{
    private readonly RaceDayApiClient _api;
    public DashboardController(RaceDayApiClient api) => _api = api;
    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("ApiCookie"))) return RedirectToAction("Login", "Account");
        ViewBag.UserName = HttpContext.Session.GetString("UserName");
        ViewBag.Role = HttpContext.Session.GetString("Role");
        ViewBag.Events = await _api.Get<List<EventViewModel>>("api/events") ?? new();
        return View();
    }
}
