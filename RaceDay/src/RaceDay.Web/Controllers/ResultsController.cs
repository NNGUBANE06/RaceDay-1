using Microsoft.AspNetCore.Mvc;using RaceDay.Web.Models;using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class ResultsController:Controller{
 readonly RaceDayApiClient api;public ResultsController(RaceDayApiClient a)=>api=a;
 [HttpGet]public async Task<IActionResult> ForEvent(int eventId){if(HttpContext.Session.GetString("Role")!="Organiser")return Forbid();ViewBag.Event=await api.Get<EventViewModel>($"api/events/{eventId}");ViewBag.EventId=eventId;ViewBag.Enrolments=await api.Get<List<EnrolmentViewModel>>($"api/events/{eventId}/enrolments")??new();return View(await api.Get<List<ResultViewModel>>($"api/events/{eventId}/results")??new());}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Capture(int eventId,ResultFormModel m){if(HttpContext.Session.GetString("Role")!="Organiser")return Forbid();if(!TimeSpan.TryParse(m.FinishTime,out var finish)){TempData["Message"]="Enter finish time as hh:mm:ss.";return RedirectToAction(nameof(ForEvent),new{eventId});}var r=await api.Send($"api/events/{eventId}/results",HttpMethod.Post,new{m.EnrolmentId,FinishTime=finish, m.Position});TempData["Message"]=r.Ok?"Result recorded.":"Could not record result. Check enrolment and position.";return RedirectToAction(nameof(ForEvent),new{eventId});}
 public async Task<IActionResult> Mine(){if(HttpContext.Session.GetString("Role")!="Participant")return Forbid();return View(await api.Get<List<ResultViewModel>>("api/results/mine")??new());}
}
