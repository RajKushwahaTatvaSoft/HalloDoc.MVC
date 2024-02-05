using Microsoft.AspNetCore.Mvc;

namespace HalloDoc.MVC.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View("Authentication/Login");
        }

        public IActionResult ForgetPassword()
        {
            return View("Authentication/ForgetPassword");
        }

        public IActionResult SubmitRequest()
        {
            return View("Request/SubmitRequest");
        }

        public IActionResult PatientRequest()
        {
            return View("Request/PatientRequest");
        }

        public IActionResult FamilyFriendRequest()
        {
            return View("Request/FamilyFriendRequest");
        }

        public IActionResult ConciergeRequest()
        {
            return View("Request/ConciergeRequest");
        }

        public IActionResult BusinessRequest()
        {
            return View("Request/BusinessRequest");
        }
    }
}
