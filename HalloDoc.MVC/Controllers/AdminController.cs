using Microsoft.AspNetCore.Mvc;

namespace HalloDoc.MVC.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View("Dashboard/Dashboard");
        }

        public IActionResult ViewCase()
        {
            return View("Dashboard/ViewCase");
        }
    }
}
