using Business_Layer.Interface;
using Data_Layer.CustomModels;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using HalloDoc.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HalloDoc.MVC.Controllers
{
    public class GuestController : Controller
    {
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        public GuestController(IUnitOfWork unitOfWork, IJwtService jwt)
        {
            _jwtService = jwt;
            _unitOfWork = unitOfWork;
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }


        public static string GenerateSHA256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var hashEngine = SHA256.Create())
            {
                var hashedBytes = hashEngine.ComputeHash(bytes, 0, bytes.Length);
                var sb = new StringBuilder();
                foreach (var b in hashedBytes)
                {
                    var hex = b.ToString("x2");
                    sb.Append(hex);
                }
                return sb.ToString();
            }
        }



        // GET
        public IActionResult PatientLogin()
        {
            return View("Patient/PatientLogin");
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientLogin(Aspnetuser loginUser)
        {
            if (ModelState.IsValid)
            {
                var passHash = GenerateSHA256(loginUser.Passwordhash);
                Aspnetuser aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(aspnetuser => aspnetuser.Username == loginUser.Username && aspnetuser.Passwordhash == passHash);

                if (aspUser != null)
                {

                    User patientUser = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                    TempData["success"] = "Login Successful";

                    SessionUser sessionUser = new SessionUser()
                    {
                        UserId = patientUser.Userid,
                        Email = patientUser.Email,
                        RoleId = (int)AllowRole.Patient,
                        UserName = patientUser.Firstname + (String.IsNullOrEmpty(patientUser.Lastname) ? "" : patientUser.Lastname),
                    };

                    var jwtToken = _jwtService.GenerateJwtToken(sessionUser);
                    Response.Cookies.Append("hallodoc", jwtToken);
                    HttpContext.Session.SetInt32("userId", patientUser.Userid);
                    return RedirectToAction("Dashboard", "Patient");
                }

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


                var jwtToken = _jwtService.GenerateJwtToken(user);
                Response.Cookies.Append("hallodoc", jwtToken);
                HttpContext.Session.SetInt32("adminId",1);

                return RedirectToAction("Dashboard", "Admin");

            }
            TempData["error"] = "Invalid Username or Password";

            return View();

        }

    }
}
