using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class EventsController : Controller
{
 private readonly RaceDayApiClient _api; public EventsController(RaceDayApiClient api)=>_api=api;
 private bool SignedIn()=>!string.IsNullOrEmpty(HttpContext.Session.GetString("ApiCookie"));
 private bool Organiser()=>HttpContext.Session.GetString("Role")=="Organiser";
 public async Task<IActionResult> Index(){if(!SignedIn())return RedirectToAction("Login","Account");return View(await _api.Get<List<EventViewModel>>("api/events")??new());}
 public async Task<IActionResult> Details(int id){if(!SignedIn())return RedirectToAction("Login","Account");ViewBag.Categories=await _api.Get<List<CategoryViewModel>>($"api/events/{id}/categories")??new();var e=await _api.Get<EventViewModel>($"api/events/{id}");return e==null?NotFound():View(e);}
 [HttpGet] public IActionResult Create(){if(!Organiser())return Forbid();return View("Form",new EventFormModel());}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(EventFormModel m){if(!Organiser())return Forbid();if(!ModelState.IsValid)return View("Form",m);var r=await _api.Send("api/events",HttpMethod.Post,new {m.Name,m.Description,m.Date,m.Location,m.DistanceKm,EventType=m.EventType});if(!r.Ok){ModelState.AddModelError("","Could not create event: "+r.Error);return View("Form",m);}return RedirectToAction(nameof(Index));}
 [HttpGet] public async Task<IActionResult> Edit(int id){if(!Organiser())return Forbid();var e=await _api.Get<EventViewModel>($"api/events/{id}");return e==null?NotFound():View("Form",new EventFormModel{Id=e.Id,Name=e.Name,Description=e.Description,Date=e.Date,Location=e.Location,DistanceKm=e.DistanceKm,EventType=e.EventType});}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit(EventFormModel m){if(!Organiser())return Forbid();if(!ModelState.IsValid)return View("Form",m);var r=await _api.Send($"api/events/{m.Id}",HttpMethod.Put,new {m.Name,m.Description,m.Date,m.Location,m.DistanceKm,EventType=m.EventType});if(!r.Ok){ModelState.AddModelError("","Could not update event: "+r.Error);return View("Form",m);}return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id){if(!Organiser())return Forbid();var r=await _api.Send($"api/events/{id}",HttpMethod.Delete);TempData["Message"]=r.Ok?"Event deleted.":"Could not delete event. It may have enrolments.";return RedirectToAction(nameof(Index));}
}
