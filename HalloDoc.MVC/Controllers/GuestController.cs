using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Data_Layer.ViewModels.Admin;
using HalloDoc.MVC.Services;
using System.IdentityModel.Tokens.Jwt;
using Business_Layer.Utilities;
using AspNetCoreHero.ToastNotification.Abstractions;
using Business_Layer.Repository.IRepository;
using Business_Layer.Services.Helper.Interface;
using Microsoft.AspNetCore.Authorization;
using Business_Layer.Services.Guest.Interface;
using System.Text.Json.Nodes;
using System.Transactions;
using Data_Layer.ViewModels.Guest;


namespace HalloDoc.MVC.Controllers
{
    public class GuestController : Controller
    {
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        private readonly IUtilityService _utilityService;
        private readonly INotyfService _notyf;
        private readonly IRequestService _requestService;
        private readonly ILogger _logger;

        public GuestController(IUnitOfWork unitOfWork, IJwtService jwt, IWebHostEnvironment environment, IConfiguration config, IUtilityService utilityService, INotyfService notyf, IRequestService requestService)
        {
            _jwtService = jwt;
            _unitOfWork = unitOfWork;
            _environment = environment;
            _config = config;
            _utilityService = utilityService;
            _notyf = notyf;
            _requestService = requestService;
        }

        public IActionResult Index()
        {
            var token = HttpContext.Request.Cookies["hallodoc"];
            if (token == null)
            {
                return View();
            }

            bool isTokenValid = _jwtService.ValidateToken(token, out JwtSecurityToken jwtToken);
            if (!isTokenValid)
            {
                return View();
            }

            var roleClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "accountTypeId");
            int roleId = Convert.ToInt32(roleClaim?.Value);

            if (roleId == (int)AccountType.Patient)
            {
                return RedirectToAction("Dashboard", "Patient");
            }
            else if (roleId == (int)AccountType.Physician)
            {
                return RedirectToAction("Dashboard", "Physician");
            }
            else if (roleId == (int)AccountType.Admin)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return View();

        }


        [HttpPost]
        public IEnumerable<City> GetCitiesByRegion(int regionId)
        {
            return _utilityService.GetCitiesByRegion(regionId);
        }

        // email token isdeleted createddate aspnetuserid expirydate
        [HttpGet]
        public IActionResult CreateAccount(string token)
        {
            try
            {
                if (ValidatePassToken(token, false))
                {
                    ForgotPasswordViewModel fpvm = new ForgotPasswordViewModel();
                    Passtoken pass = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Uniquetoken == token);

                    fpvm.Email = pass.Email;

                    return View("Patient/CreateAccount", fpvm);
                }
                else
                {
                    return View("Index");
                }

            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAccount(ForgotPasswordViewModel fpvm)
        {

            Aspnetuser user = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(u => u.Email == fpvm.Email);

            if (user == null)
            {
                TempData["error"] = "User doesn't exists. Please try again.";
                return NotFound("Error");
            }

            if (ModelState.IsValid)
            {
                string passHash = GenerateSHA256(fpvm.Password);
                user.Passwordhash = passHash;
                user.Modifieddate = DateTime.Now;

                _unitOfWork.AspNetUserRepository.Update(user);
                _unitOfWork.Save();

                Passtoken token = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Email == fpvm.Email);
                token.Isdeleted = true;
                _unitOfWork.PassTokenRepository.Update(token);
                _unitOfWork.Save();

                TempData["success"] = "Account Successfully Created.";
                return RedirectToAction("Login");

            }

            return View();
        }

        public void SendMailForCreateAccount(string email)
        {
            try
            {
                Aspnetuser aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Email == email);

                string createAccToken = Guid.NewGuid().ToString();

                Passtoken passtoken = new Passtoken()
                {
                    Aspnetuserid = aspUser.Id,
                    Createddate = DateTime.Now,
                    Email = email,
                    Isdeleted = false,
                    Isresettoken = false,
                    Uniquetoken = createAccToken,
                };

                _unitOfWork.PassTokenRepository.Add(passtoken);
                _unitOfWork.Save();

                var createLink = Url.Action("CreateAccount", "Guest", new { token = createAccToken }, Request.Scheme);

                string senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
                string senderPassword = _config.GetSection("OutlookSMTP")["Password"];

                SmtpClient client = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = "Set up your Account",
                    IsBodyHtml = true,
                    Body = "<h1>Create Account By clicking below</h1><a href=\"" + createLink + "\" >Create Account link</a>",
                };

                mailMessage.To.Add(email);

                client.Send(mailMessage);
                TempData["success"] = "Email has been successfully sent to " + email + " for create account link.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
        }

        public IActionResult SubmitRequest()
        {
            return View("Request/SubmitRequest");
        }

        public IActionResult AccessDenied()
        {
            ViewData["page"] = "Access Denied";
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
        public IActionResult Login()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel loginUser)
        {
            if (ModelState.IsValid)
            {
                var passHash = GenerateSHA256(loginUser.Password);
                Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(aspnetuser => aspnetuser.Email == loginUser.UserName && aspnetuser.Passwordhash == passHash);

                if (aspUser == null)
                {
                    TempData["error"] = "User doesn't exists";
                    return View();
                }

                SessionUser sessionUser = new SessionUser();
                string controller = "";

                if (aspUser.Accounttypeid == (int)AccountType.Patient)
                {

                    User? patientUser = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                    if (patientUser == null)
                    {
                        TempData["error"] = "Patient doesn't exists";
                        return View();
                    }

                    sessionUser = new SessionUser()
                    {
                        UserId = patientUser.Userid,
                        UserAspId = aspUser.Id,
                        Email = patientUser.Email,
                        AccountTypeId = aspUser.Accounttypeid,
                        RoleId = 0,
                        UserName = patientUser.Firstname + (String.IsNullOrEmpty(patientUser.Lastname) ? "" : " " + patientUser.Lastname),
                    };

                    TempData["success"] = "Patient Login Successful";

                    controller = "Patient";
                }
                else if (aspUser.Accounttypeid == (int)AccountType.Physician)
                {

                    Physician? physicianUser = _unitOfWork.PhysicianRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                    if (physicianUser == null)
                    {
                        TempData["error"] = "Physician doesn't exists";
                        return View();
                    }

                    sessionUser = new SessionUser()
                    {
                        UserId = physicianUser.Physicianid,
                        UserAspId = aspUser.Id,
                        Email = physicianUser.Email,
                        AccountTypeId = aspUser.Accounttypeid ,
                        RoleId = physicianUser.Roleid ?? 0,
                        UserName = physicianUser.Firstname + (String.IsNullOrEmpty(physicianUser.Lastname) ? "" : " " + physicianUser.Lastname),
                    };

                    TempData["success"] = "Physician Login Successful";

                    controller = "Physician";
                }
                else if (aspUser.Accounttypeid == (int)AccountType.Admin)
                {

                    Admin? adminUser = _unitOfWork.AdminRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                    if (adminUser == null)
                    {
                        TempData["error"] = "Admin doesn't exists";
                        return View();
                    }

                    sessionUser = new SessionUser()
                    {
                        UserId = adminUser.Adminid,
                        UserAspId = aspUser.Id,
                        Email = adminUser.Email,
                        AccountTypeId = aspUser.Accounttypeid ,
                        RoleId = adminUser.Roleid ?? 0,
                        UserName = adminUser.Firstname + (String.IsNullOrEmpty(adminUser.Lastname) ? "" : " " + adminUser.Lastname),
                    };

                    controller = "Admin";

                    //TempData["success"] = "Admin Login Successful";

                    _notyf.Success("Login Successfull", 3);
                }


                string jwtToken = _jwtService.GenerateJwtToken(sessionUser);
                Response.Cookies.Append("hallodoc", jwtToken);

                return RedirectToAction("Dashboard", controller);
            }


            TempData["error"] = "Invalid Username or Password";

            return View();

        }


        //GET
        public IActionResult PatientRequest()
        {
            PatientRequestViewModel model = new PatientRequestViewModel()
            {
                regions = _unitOfWork.RegionRepository.GetAll(),
            };

            return View("Request/PatientRequest", model);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientRequest(PatientRequestViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {
                ServiceResponse result = _requestService.SubmitPatientRequest(userViewModel);

                if (result.StatusCode == ResponseCode.Success)
                {
                    _notyf.Success(result.Message);

                    return RedirectToAction("Login");
                }
                else
                {
                    _notyf.Error(result.Message);
                }
            }

            userViewModel.Phone = "+" + userViewModel.Countrycode + '-' + userViewModel.Phone;
            userViewModel.regions = _unitOfWork.RegionRepository.GetAll();
            userViewModel.IsValidated = true;
            return View("Request/PatientRequest", userViewModel);

        }

        public void InsertRequestWiseFile(IFormFile document)
        {
            string path = _environment.WebRootPath + "/document/patient";
            string fileName = document.FileName;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            document.CopyTo(stream);
        }

        public static string GetRequestIP()
        {
            string ip = "127.0.0.1";
            return ip;
        }

        [HttpPost]
        public JsonResult PatientCheckEmail(string email)
        {
            bool emailExists = _unitOfWork.UserRepository.IsUserWithEmailExists(email);
            return Json(new { exists = emailExists });
        }



        public IActionResult FamilyFriendRequest()
        {
            FamilyFriendRequestViewModel model = new FamilyFriendRequestViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Request/FamilyFriendRequest", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FamilyFriendRequest(FamilyFriendRequestViewModel friendViewModel)
        {

            if (ModelState.IsValid)
            {
                try
                {

                    string? createLink = Url.Action("CreateAccount", "Guest", null, Request.Scheme);
                    ServiceResponse response = _requestService.SubmitFamilyFriendRequest(friendViewModel, createLink);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success(response.Message);
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        _notyf.Error(response.Message);
                    }

                }
                catch (Exception ex)
                {
                    _notyf.Error(ex.Message);
                }
            }

            friendViewModel.regions = _unitOfWork.RegionRepository.GetAll();
            friendViewModel.IsValidated = true;

            return View("Request/FamilyFriendRequest", friendViewModel);
        }

        public IActionResult ConciergeRequest()
        {
            ConciergeRequestViewModel model = new ConciergeRequestViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Request/ConciergeRequest", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConciergeRequest(ConciergeRequestViewModel conciergeViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    string? createLink = Url.Action("CreateAccount", "Guest", null, Request.Scheme);
                    ServiceResponse response = _requestService.SubmitConciergeRequest(conciergeViewModel, createLink);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success(response.Message);
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        _notyf.Error(response.Message);
                    }

                }
                catch (Exception ex)
                {
                    _notyf.Error(ex.Message);
                }
            }

            conciergeViewModel.regions = _unitOfWork.RegionRepository.GetAll();
            conciergeViewModel.IsValidated = true;
            return View("Request/ConciergeRequest", conciergeViewModel);
        }

        public IActionResult BusinessRequest()
        {
            BusinessRequestViewModel model = new BusinessRequestViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Request/BusinessRequest", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BusinessRequest(BusinessRequestViewModel businessViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    string? createLink = Url.Action("CreateAccount", "Guest", null, Request.Scheme);
                    ServiceResponse response = _requestService.SubmitBusinessRequest(businessViewModel, createLink);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success(response.Message);
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        _notyf.Error(response.Message);
                    }

                }
                catch (Exception ex)
                {
                    _notyf.Error(ex.Message);
                }

            }

            businessViewModel.regions = _unitOfWork.RegionRepository.GetAll();
            businessViewModel.IsValidated = true;
            return View("Request/BusinessRequest", businessViewModel);
        }

        public IActionResult ReviewAgreement(string requestId)
        {
            try
            {
                int decryptedId = Convert.ToInt32(EncryptionService.Decrypt(requestId.Trim()));

                Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == decryptedId);

                if (req.Status != (short)RequestStatus.Accepted)
                {
                    TempData["error"] = "Request is no longer in pending state.";
                    return View("Index");
                }

                Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(cli => cli.Requestid == decryptedId);


                SendAgreementViewModel model = new()
                {
                    requestId = decryptedId,
                    PatientName = client.Firstname + (String.IsNullOrEmpty(client.Lastname) ? "" : " " + client.Lastname),
                };
                return View(model);

            }
            catch (Exception e)
            {
                _notyf.Error(e.Message);
                return View("Index");
            }
        }


        [HttpPost]
        public bool AcceptAgreement(int requestId)
        {
            Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(req => req.Requestid == requestId);

            if (client == null)
            {
                TempData["error"] = "Cannot find the request";
                return false;
            }

            string clientName = client.Firstname + client.Lastname != null ? " " + client.Lastname : "";
            try
            {

                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (req == null)
                {
                    TempData["error"] = "Request not found. Please try again later.";
                    return false;
                }

                req.Status = (short)RequestStatus.MDEnRoute;
                req.Modifieddate = currentTime;

                string logNotes = clientName + " accepted the agreement on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss");

                Requeststatuslog statuslog = new Requeststatuslog()
                {
                    Requestid = req.Requestid,
                    Status = (short)RequestStatus.MDEnRoute,
                    Notes = logNotes,
                    Createddate = currentTime,
                    Ip = GetRequestIP(),
                };

                _unitOfWork.RequestStatusLogRepository.Add(statuslog);
                _unitOfWork.RequestRepository.Update(req);

                _unitOfWork.Save();

                TempData["success"] = "Agreement Accepted Successfully.";

                return true;

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message.ToString();
                return false;
            }
        }

        [HttpPost]
        public bool CancelAgreement(int requestId, string reason)
        {
            Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(req => req.Requestid == requestId);


            if (client == null)
            {
                TempData["error"] = "Cannot find the request";
                return false;
            }



            string clientName = client.Firstname + client.Lastname != null ? " " + client.Lastname : "";

            try
            {

                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (req == null)
                {
                    TempData["error"] = "Request not found. Please try again later.";
                    return false;
                }

                req.Status = (short)RequestStatus.CancelledByPatient;
                req.Modifieddate = currentTime;

                string logNotes = clientName + " denied the agreement on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + reason;

                Requeststatuslog statuslog = new Requeststatuslog()
                {
                    Requestid = req.Requestid,
                    Status = (short)RequestStatus.CancelledByPatient,
                    Notes = logNotes,
                    Createddate = currentTime,
                    Ip = GetRequestIP(),
                };

                _unitOfWork.RequestStatusLogRepository.Add(statuslog);
                _unitOfWork.RequestRepository.Update(req);

                _unitOfWork.Save();

                TempData["success"] = "Agreement Cancelled Successfully.";
                return true;

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message.ToString();
                return false;
            }
        }


        public string GenerateConfirmationNumber(User user)
        {
            string regionAbbr = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == user.Regionid).Abbreviation;

            DateTime todayStart = DateTime.Now.Date;
            int count = _unitOfWork.RequestRepository.Where(req => req.Createddate > todayStart).Count();

            string confirmationNumber = regionAbbr + user.Createddate.Day.ToString("D2") + user.Createddate.Month.ToString("D2") + (user.Lastname?.Substring(0, 2).ToUpper() ?? "NA") + user.Firstname.Substring(0, 2).ToUpper() + (count + 1).ToString("D4");
            return confirmationNumber;
        }


        // email token isdeleted createddate aspnetuserid expirydate
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            try
            {
                if (ValidatePassToken(token, true))
                {
                    ForgotPasswordViewModel fpvm = new ForgotPasswordViewModel();
                    Passtoken pass = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Uniquetoken == token);

                    fpvm.Email = pass.Email;

                    return View("Patient/ResetPassword", fpvm);
                }
                else
                {
                    return View("Index");
                }

            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        private bool ValidatePassToken(string token, bool isResetToken)
        {
            if (token == null)
            {
                TempData["error"] = "Invalid Token. Cannot Reset Password";
                return false;
            }

            Passtoken passtoken = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Uniquetoken == token);
            if (passtoken == null || passtoken.Isresettoken != isResetToken || passtoken.Isdeleted)
            {
                TempData["error"] = "Invalid Token. Cannot Reset Password";
                return false;
            }

            TimeSpan diff = DateTime.Now - passtoken.Createddate;
            if (diff.Hours > 24)
            {
                TempData["error"] = "Token Expired. Cannot Reset Password";
                return false;
            }

            return true;
        }

        [HttpPost]
        public IActionResult ResetPassword(ForgotPasswordViewModel fpvm)
        {
            Aspnetuser user = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(u => u.Email == fpvm.Email);

            if (user == null)
            {
                TempData["error"] = "User doesn't exists. Please try again.";
                return NotFound("Error");
            }

            if (ModelState.IsValid)
            {
                string passHash = GenerateSHA256(fpvm.Password);
                user.Passwordhash = passHash;
                user.Modifieddate = DateTime.Now;

                _unitOfWork.AspNetUserRepository.Update(user);
                _unitOfWork.Save();

                Passtoken token = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Email == fpvm.Email);
                token.Isdeleted = true;

                _unitOfWork.PassTokenRepository.Update(token);
                _unitOfWork.Save();

                TempData["success"] = "Password Reset Successful";
                return RedirectToAction("Login");

            }
            return View("Patient/ResetPassword");
        }

        public IActionResult ForgetPassword()
        {
            return View("Patient/ForgetPassword");
        }

        public IActionResult SendMailForForgetPassword(string email)
        {
            try
            {
                Aspnetuser aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Email == email);

                string resetPassToken = Guid.NewGuid().ToString();

                Passtoken passtoken = new Passtoken()
                {
                    Aspnetuserid = aspUser.Id,
                    Createddate = DateTime.Now,
                    Email = email,
                    Isdeleted = false,
                    Isresettoken = true,
                    Uniquetoken = resetPassToken,
                };

                _unitOfWork.PassTokenRepository.Add(passtoken);
                _unitOfWork.Save();

                var resetLink = Url.Action("ResetPassword", "Guest", new { token = resetPassToken }, Request.Scheme);

                string senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
                string senderPassword = _config.GetSection("OutlookSMTP")["Password"];

                SmtpClient client = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = "Set up your Account",
                    IsBodyHtml = true,
                    Body = "<h1>Hello , world!!</h1><a href=\"" + resetLink + "\" >reset pass link</a>",
                };

                mailMessage.To.Add(email);

                client.Send(mailMessage);
                TempData["success"] = "Mail sent successfully. Please check " + email + " for reset password link.";
                return RedirectToAction("ForgetPassword", "Guest");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("ForgetPassword", "Guest");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgetPassword(ForgotPasswordViewModel forPasVM)
        {
            if (ModelState.IsValid)
            {
                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(forPasVM.Email);
                if (isUserExists)
                {
                    return SendMailForForgetPassword(forPasVM.Email);
                }
            }
            TempData["error"] = "User doesn't exist.";
            return View("Patient/ForgetPassword");

        }

        #region HelperFunctions


        [HttpPost]
        public JsonArray GetBusinessByType(int professionType)
        {
            JsonArray result = new JsonArray();
            IEnumerable<Healthprofessional> businesses = _unitOfWork.HealthProfessionalRepo.Where(prof => prof.Profession == professionType);

            foreach (Healthprofessional business in businesses)
            {
                result.Add(new { businessId = business.Vendorid, businessName = business.Vendorname });
            }

            return result;
        }


        [HttpPost]
        public Healthprofessional? GetBusinessDetailsById(int vendorId)
        {
            if (vendorId <= 0)
            {
                return null;
            }
            Healthprofessional? business = _unitOfWork.HealthProfessionalRepo.GetFirstOrDefault(prof => prof.Vendorid == vendorId);

            return business;
        }


        [HttpPost]
        public List<Physician> GetPhyByRegion(int id)
        {
            return _unitOfWork.PhysicianRepository.Where(a => a.Regionid == id).ToList();
        }

        #endregion


    }
}