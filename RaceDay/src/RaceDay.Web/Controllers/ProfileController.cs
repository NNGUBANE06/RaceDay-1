using Microsoft.AspNetCore.Mvc;using RaceDay.Web.Models;using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class ProfileController:Controller{
 readonly RaceDayApiClient api;public ProfileController(RaceDayApiClient a)=>api=a;
 [HttpGet]public async Task<IActionResult> Index(){if(HttpContext.Session.GetString("ApiCookie") is null)return RedirectToAction("Login","Account");var p=await api.Get<ProfileViewModel>("api/profile");return p==null?View("Unavailable"):View(p);}
 [HttpGet]public async Task<IActionResult> Edit(){if(HttpContext.Session.GetString("ApiCookie") is null)return RedirectToAction("Login","Account");var p=await api.Get<ProfileViewModel>("api/profile");return p==null?NotFound():View(new ProfileFormModel{FirstName=p.FirstName,LastName=p.LastName});}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Edit(ProfileFormModel m){if(!ModelState.IsValid)return View(m);var r=await api.Send("api/profile",HttpMethod.Put,new{m.FirstName,m.LastName,m.NewPassword});if(!r.Ok){ModelState.AddModelError("","Profile update failed. "+r.Error);return View(m);}HttpContext.Session.SetString("UserName",m.FirstName);if(m.ProfileImage!=null){var upload=await api.Upload("api/profile/image",m.ProfileImage);if(!upload.Ok){ModelState.AddModelError("","Profile saved, but image upload failed: "+upload.Error);return View(m);}}TempData["Message"]="Profile updated.";return RedirectToAction(nameof(Index));}
}
