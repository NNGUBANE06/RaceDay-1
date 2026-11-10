using Microsoft.AspNetCore.Mvc; using RaceDay.Web.Models; using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class CategoriesController:Controller{
 readonly RaceDayApiClient api;public CategoriesController(RaceDayApiClient a)=>api=a;
 bool Allowed()=>HttpContext.Session.GetString("Role")=="Organiser";
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(int eventId,CategoryFormModel m){if(!Allowed())return Forbid();if(!ModelState.IsValid)return RedirectToAction("Details","Events",new{id=eventId});var r=await api.Send($"api/events/{eventId}/categories",HttpMethod.Post,m);TempData["Message"]=r.Ok?"Category added.":"Could not add category.";return RedirectToAction("Details","Events",new{id=eventId});}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int eventId,int id){if(!Allowed())return Forbid();var r=await api.Send($"api/events/{eventId}/categories/{id}",HttpMethod.Delete);TempData["Message"]=r.Ok?"Category removed.":"Category could not be removed.";return RedirectToAction("Details","Events",new{id=eventId});}
}
