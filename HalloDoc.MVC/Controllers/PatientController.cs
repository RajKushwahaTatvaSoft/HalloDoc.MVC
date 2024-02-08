using Data_Layer.DataContext;
using Microsoft.AspNetCore.Mvc;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;

namespace HalloDoc.MVC.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController ( ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET
        public IActionResult Login()
        {
            return View("Authentication/Login");
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(Aspnetuser loginUser)
        {

            var obj = _context.Aspnetusers.ToList();

            foreach (var aspnetuser in obj)
            {
                if(aspnetuser.Username == loginUser.Username && aspnetuser.Passwordhash == loginUser.Passwordhash)
                {
                    return View("Index");
                }
            }

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
            PatientRequestViewModel prvm = new PatientRequestViewModel();
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
