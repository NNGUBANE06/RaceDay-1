using Microsoft.AspNetCore.Mvc;using RaceDay.Web.Models;using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class EnrolmentsController:Controller{
 readonly RaceDayApiClient api;public EnrolmentsController(RaceDayApiClient a)=>api=a;
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Enrol(int eventId,EnrolFormModel m){if(HttpContext.Session.GetString("Role")!="Participant")return Forbid();var r=await api.Send($"api/events/{eventId}/enrolments",HttpMethod.Post,m);TempData["Message"]=r.Ok?"You are enrolled!": "Enrolment failed. You may already be enrolled or the category is invalid.";return RedirectToAction("Details","Events",new{id=eventId});}
 public async Task<IActionResult> Mine(){if(HttpContext.Session.GetString("Role")!="Participant")return Forbid();return View(await api.Get<List<EnrolmentViewModel>>("api/enrolments/mine")??new());}
 public async Task<IActionResult> ForEvent(int eventId){if(HttpContext.Session.GetString("Role")!="Organiser")return Forbid();ViewBag.EventId=eventId;ViewBag.Event=await api.Get<EventViewModel>($"api/events/{eventId}");return View(await api.Get<List<EnrolmentViewModel>>($"api/events/{eventId}/enrolments")??new());}
}
