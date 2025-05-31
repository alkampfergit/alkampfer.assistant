using Microsoft.AspNetCore.Mvc;

namespace alkampfer.assistant.host.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}