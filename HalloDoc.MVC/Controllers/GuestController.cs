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
using System.IO.IsolatedStorage;
using System.Security.Claims;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace HalloDoc.MVC.Controllers
{
    public class GuestController : Controller
    {
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IUtilityService _utilityService;
        private readonly INotyfService _notyf;
        private readonly IRequestService _requestService;

        public GuestController(IUnitOfWork unitOfWork, IJwtService jwt, IWebHostEnvironment environment, IConfiguration config, IUtilityService utilityService, INotyfService notyf, IRequestService requestService)
        {
            _jwtService = jwt;
            _unitOfWork = unitOfWork;
            _config = config;
            _utilityService = utilityService;
            _notyf = notyf;
            _requestService = requestService;
        }

        public IActionResult Index()
        {
            try
            {

                string? token = HttpContext.Request.Cookies["hallodoc"];
                if (token == null)
                {
                    return View();
                }

                bool isTokenValid = _jwtService.ValidateToken(token, out JwtSecurityToken jwtToken);
                if (!isTokenValid)
                {
                    return View();
                }

                Claim? roleClaim = jwtToken.Claims.FirstOrDefault(claims => claims.Type == "accountTypeId");
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Error");
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Review Agreement

        public IActionResult ReviewAgreement(string requestId)
        {
            try
            {
                int decryptedId = Convert.ToInt32(EncryptionService.Decrypt(requestId.Trim()));

                Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == decryptedId);
                Requestclient? client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(cli => cli.Requestid == decryptedId);

                if (req == null || client == null)
                {
                    _notyf.Error(NotificationMessage.REQUEST_NOT_FOUND);
                    return View("Index");
                }

                if (req.Status != (short)RequestStatus.Accepted)
                {
                    _notyf.Error("Request is no longer in pending state.");
                    return View("Index");
                }

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
                _notyf.Error("Cannot find the request");
                return false;
            }

            string clientName = client.Firstname + client.Lastname != null ? " " + client.Lastname : "";
            try
            {

                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (req == null)
                {
                    _notyf.Error("Request not found. Please try again later.");
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
                    Ip = RequestHelper.GetRequestIP(),
                };

                _unitOfWork.RequestStatusLogRepository.Add(statuslog);
                _unitOfWork.RequestRepository.Update(req);

                _unitOfWork.Save();

                _notyf.Success("Agreement Accepted Successfully.");

                return true;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        [HttpPost]
        public bool CancelAgreement(int requestId, string reason)
        {
            Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(req => req.Requestid == requestId);


            if (client == null)
            {
                _notyf.Error("Cannot find the request");
                return false;
            }



            string clientName = client.Firstname + client.Lastname != null ? " " + client.Lastname : "";

            try
            {

                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (req == null)
                {
                    _notyf.Error("Request not found. Please try again later.");
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
                    Ip = RequestHelper.GetRequestIP(),
                };

                _unitOfWork.RequestStatusLogRepository.Add(statuslog);
                _unitOfWork.RequestRepository.Update(req);

                _unitOfWork.Save();

                _notyf.Success("Agreement Cancelled Successfully.");
                return true;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        #endregion

        #region Create Requests

        public IActionResult SubmitRequest()
        {
            return View("Request/SubmitRequest");
        }


        //GET
        public IActionResult PatientRequest()
        {
            try
            {

                PatientRequestViewModel model = new PatientRequestViewModel()
                {
                    regions = _unitOfWork.RegionRepository.GetAll(),
                };

                return View("Request/PatientRequest", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("SubmitRequest");
            }

        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientRequest(PatientRequestViewModel userViewModel)
        {
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                userViewModel.Phone = "+" + userViewModel.Countrycode + '-' + userViewModel.Phone;
                userViewModel.regions = _unitOfWork.RegionRepository.GetAll();
                userViewModel.IsValidated = true;
                return View("Request/PatientRequest", userViewModel);
            }

        }

        public IActionResult FamilyFriendRequest()
        {
            try
            {
                FamilyFriendRequestViewModel model = new FamilyFriendRequestViewModel();
                model.regions = _unitOfWork.RegionRepository.GetAll();
                return View("Request/FamilyFriendRequest", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("SubmitRequest");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FamilyFriendRequest(FamilyFriendRequestViewModel friendViewModel)
        {
            try
            {


                if (ModelState.IsValid)
                {

                    string? createLink = Url.Action("CreateAccount", "Guest", null, Request.Scheme);
                    ServiceResponse response = _requestService.SubmitFamilyFriendRequest(friendViewModel, createLink);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success(response.Message);
                        return RedirectToAction("Index");
                    }

                    _notyf.Error(response.Message);
                    friendViewModel.regions = _unitOfWork.RegionRepository.GetAll();
                    friendViewModel.IsValidated = true;

                    return View("Request/FamilyFriendRequest", friendViewModel);

                }

                _notyf.Error("Please enter valid details");
                friendViewModel.regions = _unitOfWork.RegionRepository.GetAll();
                friendViewModel.IsValidated = true;

                return View("Request/FamilyFriendRequest", friendViewModel);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                friendViewModel.regions = _unitOfWork.RegionRepository.GetAll();
                friendViewModel.IsValidated = true;
                return View("Request/FamilyFriendRequest", friendViewModel);
            }
        }

        public IActionResult ConciergeRequest()
        {
            try
            {

                ConciergeRequestViewModel model = new ConciergeRequestViewModel();
                model.regions = _unitOfWork.RegionRepository.GetAll();
                return View("Request/ConciergeRequest", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("SubmitRequest");
            }
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
            try
            {
                BusinessRequestViewModel model = new BusinessRequestViewModel();
                model.regions = _unitOfWork.RegionRepository.GetAll();
                return View("Request/BusinessRequest", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("SubmitRequest");
            }
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


        #endregion

        #region Account Based

        #region Login


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
            try
            {


                if (ModelState.IsValid)
                {
                    var passHash = AuthHelper.GenerateSHA256(loginUser.Password);
                    Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(aspnetuser => aspnetuser.Email == loginUser.UserName && aspnetuser.Passwordhash == passHash);

                    if (aspUser == null)
                    {
                        _notyf.Error("User doesn't exists");
                        return View();
                    }

                    SessionUser sessionUser = new SessionUser();
                    string controller = "";

                    switch (aspUser.Accounttypeid)
                    {
                        case (int)AccountType.Patient:
                            break;

                        case (int)AccountType.Physician:
                            break;

                        case (int)AccountType.Admin:
                            break;
                    }

                    if (aspUser.Accounttypeid == (int)AccountType.Patient)
                    {

                        User? patientUser = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                        if (patientUser == null)
                        {
                            _notyf.Error("Patient doesn't exists");
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


                        controller = "Patient";
                    }
                    else if (aspUser.Accounttypeid == (int)AccountType.Physician)
                    {

                        Physician? physicianUser = _unitOfWork.PhysicianRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                        if (physicianUser == null)
                        {
                            _notyf.Error("Physician doesn't exists");
                            return View();
                        }

                        sessionUser = new SessionUser()
                        {
                            UserId = physicianUser.Physicianid,
                            UserAspId = aspUser.Id,
                            Email = physicianUser.Email,
                            AccountTypeId = aspUser.Accounttypeid,
                            RoleId = physicianUser.Roleid ?? 0,
                            UserName = physicianUser.Firstname + (String.IsNullOrEmpty(physicianUser.Lastname) ? "" : " " + physicianUser.Lastname),
                        };


                        controller = "Physician";
                    }
                    else if (aspUser.Accounttypeid == (int)AccountType.Admin)
                    {

                        Admin? adminUser = _unitOfWork.AdminRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                        if (adminUser == null)
                        {
                            _notyf.Error("Admin doesn't exists");
                            return View();
                        }

                        sessionUser = new SessionUser()
                        {
                            UserId = adminUser.Adminid,
                            UserAspId = aspUser.Id,
                            Email = adminUser.Email,
                            AccountTypeId = aspUser.Accounttypeid,
                            RoleId = adminUser.Roleid ?? 0,
                            UserName = adminUser.Firstname + (String.IsNullOrEmpty(adminUser.Lastname) ? "" : " " + adminUser.Lastname),
                        };

                        controller = "Admin";

                    }

                    _notyf.Success("Login Successfull", 3);
                    string jwtToken = _jwtService.GenerateJwtToken(sessionUser);
                    Response.Cookies.Append("hallodoc", jwtToken);

                    return RedirectToAction("Dashboard", controller);
                }

                _notyf.Error("Invalid Username or Password");

                return View();
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View();
            }

        }


        #endregion

        #region Forget Password


        // email token isdeleted createddate aspnetuserid expirydate
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            try
            {
                bool isValidToken = _unitOfWork.PassTokenRepository.ValidatePassToken(token, true, out string errorMessage);

                if (!isValidToken)
                {
                    _notyf.Error(errorMessage);
                    return View("Index");
                }

                ForgotPasswordViewModel fpvm = new ForgotPasswordViewModel();
                Passtoken? pass = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Uniquetoken == token);

                fpvm.Email = pass?.Email;

                return View("Patient/ResetPassword", fpvm);

            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult ResetPassword(ForgotPasswordViewModel fpvm)
        {
            try
            {
                Aspnetuser? user = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(u => u.Email == fpvm.Email);

                if (user == null)
                {
                    _notyf.Error("User doesn't exists. Please try again.");
                    return NotFound("Error");
                }

                if (ModelState.IsValid)
                {

                    string passHash = AuthHelper.GenerateSHA256(fpvm.Password ?? "");
                    user.Passwordhash = passHash;
                    user.Modifieddate = DateTime.Now;

                    _unitOfWork.AspNetUserRepository.Update(user);
                    _unitOfWork.Save();

                    Passtoken? token = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Email == fpvm.Email);
                    token.Isdeleted = true;

                    _unitOfWork.PassTokenRepository.Update(token);
                    _unitOfWork.Save();

                    _notyf.Success("Password Reset Successful");
                    return RedirectToAction("Login");

                }
                return View("Patient/ResetPassword", fpvm);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Patient/ResetPassword", fpvm);
            }
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
                _notyf.Success("Mail sent successfully. Please check " + email + " for reset password link.");
                return RedirectToAction("ForgetPassword", "Guest");
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("ForgetPassword", "Guest");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgetPassword(ForgotPasswordViewModel forPasVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(forPasVM.Email ?? "");
                    if (isUserExists)
                    {
                        return SendMailForForgetPassword(forPasVM.Email ?? "");
                    }
                }
                _notyf.Error("User doesn't exist.");
                return View("Patient/ForgetPassword", forPasVM);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Patient/ForgetPassword", forPasVM);

            }

        }


        #endregion

        #region Create Patient Account

        // email token isdeleted createddate aspnetuserid expirydate
        [HttpGet]
        public IActionResult CreateAccount(string token)
        {
            try
            {
                bool isTokenValid = _unitOfWork.PassTokenRepository.ValidatePassToken(token, false, out string errorMessage);

                if (!isTokenValid)
                {
                    _notyf.Error(errorMessage);
                    return View("Index");
                }

                ForgotPasswordViewModel fpvm = new ForgotPasswordViewModel();
                Passtoken? pass = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Uniquetoken == token);

                fpvm.Email = pass?.Email;

                return View("Patient/CreateAccount", fpvm);


            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAccount(ForgotPasswordViewModel fpvm)
        {
            try
            {
                Aspnetuser? user = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(u => u.Email == fpvm.Email);

                if (user == null)
                {
                    _notyf.Error("User doesn't exists. Please try again.");
                    return NotFound("Error");
                }

                if (ModelState.IsValid)
                {
                    string passHash = AuthHelper.GenerateSHA256(fpvm.Password);
                    user.Passwordhash = passHash;
                    user.Modifieddate = DateTime.Now;

                    _unitOfWork.AspNetUserRepository.Update(user);
                    _unitOfWork.Save();

                    Passtoken? token = _unitOfWork.PassTokenRepository.GetFirstOrDefault(pass => pass.Email == fpvm.Email);

                    if (token == null)
                    {
                        _notyf.Error("Token not found");
                        return RedirectToAction("Login");
                    }

                    token.Isdeleted = true;
                    _unitOfWork.PassTokenRepository.Update(token);
                    _unitOfWork.Save();

                    _notyf.Success("Account Successfully Created.");
                    return RedirectToAction("Login");

                }

                return View();

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Index");
            }
        }


        #endregion

        #endregion

        #region HelperFunctions

        [HttpPost]
        public JsonResult PatientCheckEmail(string email)
        {
            bool emailExists = _unitOfWork.AspNetUserRepository.IsUserWithEmailExists(email);
            return Json(new { exists = emailExists });
        }

        [HttpGet]
        public ActionResult GetMenusByAccounttype(short type)
        {
            List<Menu> checkboxItems;
            checkboxItems = _unitOfWork.MenuRepository.Where(x => x.Accounttype == type).ToList();

            return Ok(checkboxItems);

        }


        [HttpPost]
        public IEnumerable<City> GetCitiesByRegion(int regionId)
        {
            return _utilityService.GetCitiesByRegion(regionId);
        }

        [HttpPost]
        // Removes the physician whose physicianId is passed : reason for transfer case modal
        public IEnumerable<Physician> GetPhysicianByPhysicianRegion(int regionId, int? physicianId)
        {
            var result = new JsonArray();

            List<Physician> physicians = _unitOfWork.PhysicianRepository.GetPhysiciansByPhysicianRegion(regionId).ToList();
            Physician? removePhy = physicians.SingleOrDefault(p => p.Physicianid == physicianId);
            if (removePhy != null)
            {
                physicians.Remove(removePhy);
            }
            return physicians;
        }

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

        public IEnumerable<ChatMessage> FetchChats(string senderAspId, string receiverAspId, int requestId)
        {
            IEnumerable<ChatMessage> chatMessages = _unitOfWork.ChatMessageRepository
                .Where(chat =>
                 chat.RequestId == requestId
                && ((chat.SenderAspId == senderAspId && chat.ReceiverAspId == receiverAspId)
                 || (chat.ReceiverAspId == senderAspId && chat.SenderAspId == receiverAspId))
                )
                .OrderBy(_ => _.SentTime);
            JsonArray chatArray = new();

            foreach (ChatMessage message in chatMessages)
            {
                string formattedTime = message.SentTime.ToString("hh:mm");
                chatArray.Add(new
                {
                    messageContent = message.MessageContent,
                    sentTime = formattedTime,
                    senderAspId = message.SenderAspId,
                });
            }

            return chatMessages;
        }

        public JsonArray FetchGroupChats(int requestId)
        {

            string groupName = $"GroupForRequest{requestId}";

            IEnumerable<ChatMessage> chatMessages = _unitOfWork.ChatMessageRepository
                .Where(chat =>
                 chat.RequestId == requestId && chat.ReceiverAspId == groupName
                )
                .OrderBy(_ => _.SentTime).ToList();

            JsonArray chatArray = new();

            IEnumerable<string> distinctAspIds = chatMessages.Select(_ => _.SenderAspId).Distinct();
            List<KeyValuePair<string, string>> aspIdWithImagePaths = new List<KeyValuePair<string, string>>();

            foreach (string aspId in distinctAspIds)
            {
                Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Id == aspId);

                if (aspUser == null)
                {
                    aspIdWithImagePaths.Add(new KeyValuePair<string, string>(aspId, Constants.DEFAULT_PATIENT_IMAGE_PATH));
                    continue;
                }
                KeyValuePair<string, string> aspIdImage;
                switch ((AccountType)aspUser.Accounttypeid)
                {
                    case AccountType.Admin:
                        aspIdImage = new KeyValuePair<string, string>(aspId, Constants.DEFAULT_ADMIN_IMAGE_PATH);
                        break;

                    case AccountType.Patient:
                        aspIdImage = new KeyValuePair<string, string>(aspId, Constants.DEFAULT_PATIENT_IMAGE_PATH);
                        break;

                    case AccountType.Physician:
                        {
                            aspIdImage = new KeyValuePair<string, string>(aspId, Constants.DEFAULT_PROVIDER_IMAGE_PATH);
                            break;
                        }

                    default:
                        {
                            aspIdImage = new KeyValuePair<string, string>(aspId, Constants.DEFAULT_PATIENT_IMAGE_PATH);
                            break;
                        }
                }

                aspIdWithImagePaths.Add(aspIdImage);

            }

            foreach (ChatMessage message in chatMessages)
            {
                string formattedTime = message.SentTime.ToString("hh:mm");

                Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Id == message.SenderAspId);


                chatArray.Add(new
                {
                    messageContent = message.MessageContent,
                    sentTime = formattedTime,
                    senderAspId = message.SenderAspId,
                    imagePath = aspIdWithImagePaths.FirstOrDefault(image => image.Key.Equals(message.SenderAspId)).Value,
                });
            }

            return chatArray;
        }

        public JsonResult GetNameAndImageFromAspId(string userAspId, int accountType)
        {
            Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Id.Equals(userAspId));

            if (aspUser == null || aspUser.Accounttypeid != accountType)
            {
                _notyf.Error("Cannot find user");
                return Json(new { isSuccess = false });
            }

            switch ((AccountType)accountType)
            {
                case AccountType.Admin:
                    {
                        Admin? admin = _unitOfWork.AdminRepository.GetFirstOrDefault(admin => admin.Aspnetuserid == userAspId);
                        if (admin == null)
                        {
                            _notyf.Error("Cannot find user");
                            return Json(new { isSuccess = false });
                        }

                        string adminName = $"{admin.Firstname} {admin.Lastname}";
                        string imagePath = Constants.DEFAULT_ADMIN_IMAGE_PATH;

                        return Json(new { isSuccess = true, userName = adminName, userImagePath = imagePath });
                    }

                case AccountType.Physician:
                    {

                        Physician? physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Aspnetuserid == userAspId);
                        if (physician == null)
                        {
                            _notyf.Error("Cannot find user");
                            return Json(new { isSuccess = false });
                        }

                        string phyName = $"{physician.Firstname} {physician.Lastname}";
                        string imagePath = Constants.DEFAULT_PROVIDER_IMAGE_PATH;

                        if (physician.Photo != null)
                        {
                            imagePath = $"/document/physician/{physician.Physicianid}/ProfilePhoto.jpg";
                        }

                        return Json(new { isSuccess = true, userName = phyName, userImagePath = imagePath });
                    }

                case AccountType.Patient:
                    {

                        User? patient = _unitOfWork.UserRepository.GetFirstOrDefault(user => user.Aspnetuserid == userAspId);
                        if (patient == null)
                        {
                            _notyf.Error("Cannot find user");
                            return Json(new { isSuccess = false });
                        }

                        string patientName = $"{patient.Firstname} {patient.Lastname}";
                        string imagePath = Constants.DEFAULT_PATIENT_IMAGE_PATH;

                        return Json(new { isSuccess = true, userName = patientName, userImagePath = imagePath });
                    }
            }

            return Json(new { isSuccess = false });
        }

    }
}