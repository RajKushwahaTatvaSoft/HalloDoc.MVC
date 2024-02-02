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
            return View();
        }

        public IActionResult ForgetPassword(){
            return View();
        }

        public IActionResult SubmitRequest()
        {
            return View();
        }
    }
}
