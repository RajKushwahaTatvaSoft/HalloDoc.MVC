using Business_Layer.Interface;
using Data_Layer.CustomModels;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using HalloDoc.MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace HalloDoc.MVC.Controllers
{
    public class GuestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        public GuestController(ApplicationDbContext context, IJwtService jwt)
        {
            _context = context;
            _jwtService = jwt;
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET
        public IActionResult PatientLogin()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientLogin(Aspnetuser loginUser)
        {
            if (ModelState.IsValid)
            {
                SessionUser user = new SessionUser()
                {
                    UserId = 20,
                    Email = "admin@gmail.com",
                    RoleId = (int) AllowRole.Patient,
                    UserName = "admin admin"
                };

                var jwtToken = _jwtService.GenerateJwtToken(user);
                Response.Cookies.Append("hallodoc",jwtToken);

                return RedirectToAction("Dashboard","Patient");
            }

            TempData["error"] = "Invalid Username or Password";
            return View();

        }

        // GET
        public IActionResult AdminLogin()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogin(Aspnetuser loginUser)
        {
            if (ModelState.IsValid)
            {
                SessionUser user = new SessionUser()
                {
                    UserId = 1,
                    Email = "admin@gmail.com",
                    RoleId = (int) AllowRole.Admin,
                    UserName = "admin admin"
                };

                SessionUtils.SetLoggedInUser(HttpContext.Session, user);

                return RedirectToAction("Dashboard", "Admin");

            }
            TempData["error"] = "Invalid Username or Password";

            return View();

        }

    }
}
