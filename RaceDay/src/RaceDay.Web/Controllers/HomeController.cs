using Microsoft.AspNetCore.Mvc;
namespace RaceDay.Web.Controllers;
public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() => View();
}
