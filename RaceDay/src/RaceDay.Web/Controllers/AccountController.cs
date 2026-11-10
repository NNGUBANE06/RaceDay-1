using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;
public class AccountController : Controller
{
    private readonly RaceDayApiClient _api;
    public AccountController(RaceDayApiClient api) => _api = api;

    [HttpGet] public IActionResult Login() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _api.Send("api/auth/login", HttpMethod.Post, model, saveCookie: true);
        if (!result.Ok) { ModelState.AddModelError("", "Login failed. Check your email and password."); return View(model); }
        var profile = await _api.Get<ProfileViewModel>("api/profile");
        if (profile is not null) { HttpContext.Session.SetString("UserName", profile.FirstName); HttpContext.Session.SetString("Role", profile.Role); }
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet] public IActionResult Register() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _api.Send("api/auth/register", HttpMethod.Post, model);
        if (!result.Ok) { ModelState.AddModelError("", "Registration failed. Check the details or use another email."); return View(model); }
        TempData["Message"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.Send("api/auth/logout", HttpMethod.Post);
        _api.ClearSession(); HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
