using Microsoft.AspNetCore.Mvc;

namespace PlannerApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}