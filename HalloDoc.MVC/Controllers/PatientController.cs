using Microsoft.AspNetCore.Mvc;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Mail;
using System.Net;
using Microsoft.CodeAnalysis;
using Business_Layer.Interface;
using Business_Layer.Helpers;
using HalloDoc.MVC.Services;

namespace HalloDoc.MVC.Controllers
{
    public enum RequestStatus
    {
        Unassigned = 1,
        Accepted = 2,
        Cancelled = 3,
        MDEnRoute = 4,
        MDOnSite = 5,
        Conclude = 6,
        CancelledByPatient = 7,
        Closed = 8,
        Unpaid = 9,
        Clear = 10,
        Block = 11,
    }

    public enum DashboardStatus
    {
        New = 1,
        Pending = 2,
        Active = 3,
        Conclude = 4,
        ToClose = 5,
        Unpaid = 6,
    }

    public enum RequestType
    {
        Business = 1,
        Patient = 2,
        Family = 3,
        Concierge = 4
    }

    public enum AllowRole
    {
        Admin = 1,
        Patient = 2,
        Physician = 3
    }

    [CustomAuthorize((int)AllowRole.Patient)]
    public class PatientController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        private readonly IPatientDashboardRepository _dashboardRepo;
        private readonly IPatientAuthRepository _authRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;

        public PatientController(IWebHostEnvironment environment, IConfiguration config, IPatientDashboardRepository patientDashboardRepository, IPatientAuthRepository authRepo, IUnitOfWork unitwork)
        {
            _environment = environment;
            _config = config;
            _dashboardRepo = patientDashboardRepository;
            _authRepo = authRepo;
            _unitOfWork = unitwork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PatientDashboardHeader()
        {
            return View("Dashboard/PatientDashboardHeader");
        }

        public IActionResult Dashboard()
        {
            int userId = (int) HttpContext.Session.GetInt32("userId");
            
            if (userId == null)
            {
                return View("Error");
            }

            PatientDashboardViewModel model = _dashboardRepo.FetchDashboardDetails((int)userId);

            return View("Dashboard/Dashboard", model);
        }

        public IActionResult RequestForMe()
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null)
            {
                return View("Error");
            }

            User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);

            string dobDate;

            if (user.Intyear == null || user.Strmonth == null || user.Intdate == null)
            {
                dobDate = null;
            }
            else
            {
                dobDate = user.Intyear + "-" + user.Strmonth + "-" + user.Intdate;
            }

            MeRequestViewModel model = new()
            {
                UserId = user.Userid,
                FirstName = user.Firstname,
                LastName = user.Lastname,
                DOB = dobDate == null ? null : DateTime.Parse(dobDate),
                Phone = user.Mobile,
                Email = user.Email,
                Street = user.Street,
                City = user.City,
                State = user.State,
                ZipCode = user.Zipcode,
            };


            return View("Dashboard/RequestForMe", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestForMe(MeRequestViewModel meRequestViewModel)
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null)
            {
                return View("Error");
            }

            if (ModelState.IsValid)
            {

                User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);
                string requestIpAddress = GetRequestIP();
                string phoneNumber = "+" + meRequestViewModel.Countrycode + '-' + meRequestViewModel.Phone;

                Request request = new()
                {
                    Requesttypeid = 2,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = meRequestViewModel.FirstName,
                    Lastname = meRequestViewModel.LastName,
                    Phonenumber = phoneNumber,
                    Email = meRequestViewModel.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _unitOfWork.RequestRepository.Add(request);
                _unitOfWork.Save();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = meRequestViewModel.FirstName,
                    Lastname = meRequestViewModel.LastName,
                    Phonenumber = phoneNumber,
                    Email = meRequestViewModel.Email,
                    Address = meRequestViewModel.Street,
                    City = meRequestViewModel.City,
                    State = meRequestViewModel.State,
                    Zipcode = meRequestViewModel.ZipCode,
                    Notes = meRequestViewModel.Symptom,
                    Ip = requestIpAddress,
                };

                _unitOfWork.RequestClientRepository.Add(requestclient);
                _unitOfWork.Save();

                //Adding File Data in RequestWiseFile Table
                if (meRequestViewModel.File != null)
                {
                    InsertRequestWiseFile(meRequestViewModel.File);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = meRequestViewModel.File.FileName,
                    };

                    _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                    _unitOfWork.Save();
                }


                TempData["success"] = "Request Added Successfully.";
                return RedirectToAction("Dashboard");
            }
            return View("MeRequest");
        }

        public IActionResult RequestForSomeoneElse()
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null)
            {
                return View("Error");
            }

            User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);

            SomeoneElseRequestViewModel model = new()
            {
                Username = user.Firstname + " " + user.Lastname,
            };

            return View("Dashboard/RequestForSomeoneElse", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestForSomeoneElse(SomeoneElseRequestViewModel srvm)
        {
            var token = Request.Cookies["hallodoc"];
            
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null)
            {
                return View("Error");
            }

            if (ModelState.IsValid)
            {
                bool isNewUser = _unitOfWork.UserRepository.IsUserWithEmailExists(srvm.patientDetails.Email);

                User relationUser = _unitOfWork.UserRepository.GetUserWithID((int)userId);
                string requestIpAddress = GetRequestIP();
                string phoneNumber = "+" + srvm.patientDetails.Countrycode + '-' + srvm.patientDetails.Phone;

                User user = null;

                if (isNewUser)
                {

                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = srvm.patientDetails.Email!,
                        Email = srvm.patientDetails.Email,
                        Phonenumber = phoneNumber,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                    _unitOfWork.Save();

                    // Creating Patient in User Table
                    user = new()
                    {
                        Aspnetuserid = generatedId.ToString(),
                        Firstname = srvm.patientDetails.FirstName,
                        Lastname = srvm.patientDetails.LastName,
                        Email = srvm.patientDetails.Email,
                        Mobile = phoneNumber,
                        Street = srvm.patientDetails.Street,
                        City = srvm.patientDetails.City,
                        State = srvm.patientDetails.State,
                        Zipcode = srvm.patientDetails.ZipCode,
                        Createddate = DateTime.Now,
                        Createdby = generatedId.ToString(),
                        Ip = requestIpAddress,
                        Intdate = srvm.patientDetails.DOB.Value.Day,
                        Strmonth = srvm.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = srvm.patientDetails.DOB.Value.Year,
                    };

                    _unitOfWork.UserRepository.Add(user);
                    _unitOfWork.Save();

                    SendMailForCreateAccount(srvm.patientDetails.Email);


                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Family,
                        Userid = relationUser.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = relationUser.Firstname,
                        Lastname = relationUser.Lastname,
                        Phonenumber = relationUser.Mobile,
                        Email = relationUser.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = user.Aspnetuserid,
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = srvm.patientDetails.FirstName,
                        Lastname = srvm.patientDetails.LastName,
                        Phonenumber = phoneNumber,
                        Email = srvm.patientDetails.Email,
                        Address = srvm.patientDetails.Street,
                        City = srvm.patientDetails.City,
                        State = srvm.patientDetails.State,
                        Zipcode = srvm.patientDetails.ZipCode,
                        Notes = srvm.patientDetails.Symptom,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    //Adding File Data in RequestWiseFile Table
                    if (srvm.patientDetails.File != null)
                    {
                        InsertRequestWiseFile(srvm.patientDetails.File);

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = srvm.patientDetails.File.FileName,
                        };

                        _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                        _unitOfWork.Save();
                    }

                }
                else
                {

                    user = _unitOfWork.UserRepository.GetUserWithEmail(srvm.patientDetails.Email);

                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Family,
                        Userid = relationUser.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = relationUser.Firstname,
                        Lastname = relationUser.Lastname,
                        Phonenumber = relationUser.Mobile,
                        Email = relationUser.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = user.Aspnetuserid,
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = srvm.patientDetails.FirstName,
                        Lastname = srvm.patientDetails.LastName,
                        Phonenumber = phoneNumber,
                        Email = srvm.patientDetails.Email,
                        Address = srvm.patientDetails.Street,
                        City = srvm.patientDetails.City,
                        State = srvm.patientDetails.State,
                        Zipcode = srvm.patientDetails.ZipCode,
                        Notes = srvm.patientDetails.Symptom,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    //Adding File Data in RequestWiseFile Table
                    if (srvm.patientDetails.File != null)
                    {
                        InsertRequestWiseFile(srvm.patientDetails.File);

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = srvm.patientDetails.File.FileName,
                        };

                        _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                        _unitOfWork.Save();
                    }

                }

                TempData["success"] = "Request Added Successfully";

                return RedirectToAction("PatientDashboard");

            }

            return View("Dashboard/RequestForSomeoneElse");
        }


        public IActionResult CreateAccountGet(ForgotPasswordViewModel fpvm)
        {
            return View("Authentication/CreateAccount", fpvm);
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

                    return CreateAccountGet(fpvm);
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

        public IActionResult ViewDocument(int requestId)
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null || requestId == null)
            {
                return View("Error");
            }

            User user = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Userid == userId);
            Request request = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);

            List<Requestwisefile> files = _unitOfWork.RequestWiseFileRepository.GetAll().Where(reqFile => reqFile.Requestid == requestId).ToList();

            ViewDocumentViewModel viewDocumentVM = new ViewDocumentViewModel();

            viewDocumentVM.requestwisefiles = files;
            viewDocumentVM.RequestId = requestId;
            viewDocumentVM.UserName = user.Firstname + " " + user.Lastname;
            viewDocumentVM.ConfirmationNumber = request.Confirmationnumber;

            return View("Dashboard/ViewDocument", viewDocumentVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ViewDocument(ViewDocumentViewModel viewDocumentVM)
        {
            if (viewDocumentVM.File != null)
            {
                InsertRequestWiseFile(viewDocumentVM.File);

                Requestwisefile requestwisefile = new()
                {
                    Requestid = viewDocumentVM.RequestId,
                    Filename = viewDocumentVM.File.FileName,
                    Createddate = DateTime.Now,
                    Ip = GetRequestIP(),

                };
                _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                _unitOfWork.Save();

                viewDocumentVM.File = null;

            }

            return ViewDocument(viewDocumentVM.RequestId);
        }

        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("userId");
            User? user = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Userid == userId);

            if (user != null)
            {
                string dobDate = user.Intyear + "-" + user.Strmonth + "-" + user.Intdate;

                PatientProfileViewModel model = new()
                {
                    UserId = user.Userid,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    Date = DateTime.Parse(dobDate),
                    Type = "Mobile",
                    Phone = user.Mobile,
                    Email = user.Email,
                    Street = user.Street,
                    City = user.City,
                    State = user.State,
                    ZipCode = user.Zipcode,
                };

                return View("Dashboard/Profile", model);
            }
            return RedirectToAction("Error");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(PatientProfileViewModel pm)
        {
            string phoneNumber = "+" + pm.CountryCode + '-' + pm.Phone;

            User dbUser = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Userid == pm.UserId);
            dbUser.Firstname = pm.FirstName;
            dbUser.Lastname = pm.LastName;
            dbUser.Intdate = pm.Date.Value.Day;
            dbUser.Strmonth = pm.Date.Value.Month.ToString();
            dbUser.Intyear = pm.Date.Value.Year;
            dbUser.Mobile = phoneNumber;
            dbUser.Street = pm.Street;
            dbUser.City = pm.City;
            dbUser.State = pm.State;
            dbUser.Zipcode = pm.ZipCode;

            _unitOfWork.UserRepository.Update(dbUser);
            _unitOfWork.Save();
            return RedirectToAction("Dashboard");
        }

        public IActionResult Logout()
        {

            Response.Cookies.Delete("hallodoc");
            TempData["success"] = "Logout Successfull";

            return RedirectToAction("PatientLogin","Guest");
        }

        public async Task<IActionResult> DownloadAllFiles(int requestId)
        {
            try
            {
                // Fetch all document details for the given request:
                var documentDetails = _unitOfWork.RequestWiseFileRepository.GetAll().Where(m => m.Requestid == requestId).ToList();

                if (documentDetails == null || documentDetails.Count == 0)
                {
                    return NotFound("No documents found for download");
                }

                // Create a unique zip file name
                var zipFileName = $"Documents_{DateTime.Now:yyyyMMddHHmmss}.zip";
                var zipFilePath = Path.Combine(_environment.WebRootPath, "DownloadableZips", zipFileName);

                // Create the directory if it doesn't exist
                var zipDirectory = Path.GetDirectoryName(zipFilePath);
                if (!Directory.Exists(zipDirectory))
                {
                    Directory.CreateDirectory(zipDirectory);
                }

                // Create a new zip archive
                using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    // Add each document to the zip archive
                    foreach (var document in documentDetails)
                    {
                        var documentPath = Path.Combine(_environment.WebRootPath, "document", document.Filename);
                        zipArchive.CreateEntryFromFile(documentPath, document.Filename);
                    }
                }

                // Return the zip file for download
                var zipFileBytes = await System.IO.File.ReadAllBytesAsync(zipFilePath);
                return File(zipFileBytes, "application/zip", zipFileName);
            }
            catch
            {
                return BadRequest("Error downloading files");
            }
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
            if (ModelState.IsValid)
            {
                var passHash = GenerateSHA256(loginUser.Passwordhash);
                Aspnetuser user = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(aspnetuser => aspnetuser.Username == loginUser.Username && aspnetuser.Passwordhash == passHash);

                if (user != null)
                {
                    User patientUser = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Aspnetuserid == user.Id);
                    TempData["success"] = "Login Successful";
                    HttpContext.Session.SetInt32("userId", patientUser.Userid);
                    return RedirectToAction("Dashboard");
                }

            }
            TempData["error"] = "Invalid Username or Password";

            return View("Authentication/Login");

        }

        public IActionResult ResetPasswordGet(ForgotPasswordViewModel fpvm)
        {
            return View("Authentication/ResetPassword", fpvm);
        }

        //public void JwtTokenCode(string email)
        //{
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(new[] { new Claim("email", email) }),
        //        Expires = DateTime.UtcNow.AddHours(24),
        //        Issuer = _config["Jwt:Issuer"],
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //    };
        //    var token = tokenHandler.CreateToken(tokenDescriptor);
        //    var jwtToken = tokenHandler.WriteToken(token);


        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);

        //    tokenHandler.ValidateToken(token, new TokenValidationParameters
        //    {
        //        ValidateIssuerSigningKey = true,
        //        IssuerSigningKey = new SymmetricSecurityKey(key),
        //        ValidateLifetime = true,
        //        ValidateIssuer = true,
        //        ValidateAudience = false,
        //        ValidIssuer = _config["Jwt:Issuer"],
        //        ClockSkew = TimeSpan.Zero
        //    }, out SecurityToken validatedToken);

        //    var jwtToken = (JwtSecurityToken)validatedToken;
        //    var email = jwtToken.Claims.First(x => x.Type == "email").Value;
        //}

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

                    return ResetPasswordGet(fpvm);
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
            if (passtoken == null || passtoken.Isresettoken == isResetToken || passtoken.Isdeleted)
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
            return View();
        }


        public IActionResult ForgetPassword()
        {
            return View("Authentication/ForgetPassword");
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
                    Isresettoken = true,
                    Uniquetoken = createAccToken,
                };

                _unitOfWork.PassTokenRepository.Add(passtoken);
                _unitOfWork.Save();

                var createLink = Url.Action("CreateAccount", "Patient", new { token = createAccToken }, Request.Scheme);

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

        public IActionResult SendMailForForgetPassword(string email, string onSucessAction, string onSucessController, string emailAction, string emailController, string onFailedAction, string onFailedController)
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

                var resetLink = Url.Action(emailAction, emailController, new { token = resetPassToken }, Request.Scheme);

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
                return RedirectToAction(onSucessAction, onSucessController);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(onFailedAction, onFailedController);
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
                    return SendMailForForgetPassword(forPasVM.Email, "ForgetPassword", "Patient", "ResetPassword", "Patient", "ForgetPassword", "Patient");
                }
            }
            TempData["error"] = "User doesn't exist.";
            return View("Authentication/ForgetPassword");

        }


        //GET
        public IActionResult PatientRequest()
        {
            return View("Request/PatientRequest");
        }
        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientRequest(PatientRequestViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {
                string requestIpAddress = GetRequestIP();
                string phoneNumber = "+" + userViewModel.Countrycode + '-' + userViewModel.Phone;

                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(userViewModel.Email);
                if (!isUserExists)
                {
                    if (userViewModel.Password != null)
                    {

                        User user;

                        Guid generatedId = Guid.NewGuid();

                        // Creating Patient in Aspnetusers Table
                        Aspnetuser aspnetuser = new()
                        {
                            Id = generatedId.ToString(),
                            Username = userViewModel.Email,
                            Passwordhash = AuthHelper.GenerateSHA256(userViewModel.Password),
                            Email = userViewModel.Email,
                            Phonenumber = phoneNumber,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                        };
                        _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                        _unitOfWork.Save();

                        // Creating Patient in User Table
                        user = new()
                        {
                            Aspnetuserid = generatedId.ToString(),
                            Firstname = userViewModel.FirstName,
                            Lastname = userViewModel.LastName,
                            Email = userViewModel.Email,
                            Mobile = phoneNumber,
                            Street = userViewModel.Street,
                            City = userViewModel.City,
                            State = userViewModel.State,
                            Zipcode = userViewModel.ZipCode,
                            Createddate = DateTime.Now,
                            Createdby = generatedId.ToString(),
                            Ip = requestIpAddress,
                            Intdate = userViewModel.DOB.Value.Day,
                            Strmonth = userViewModel.DOB.Value.Month.ToString(),
                            Intyear = userViewModel.DOB.Value.Year,
                        };

                        _unitOfWork.UserRepository.Add(user);
                        _unitOfWork.Save();

                        // Adding request in Request Table
                        Request request = new()
                        {
                            Requesttypeid = (int)RequestType.Patient,
                            Userid = user.Userid,
                            Confirmationnumber = GenerateConfirmationNumber(user),
                            Firstname = userViewModel.FirstName,
                            Lastname = userViewModel.LastName,
                            Phonenumber = phoneNumber,
                            Email = userViewModel.Email,
                            Status = (short)RequestStatus.Unassigned,
                            Createddate = DateTime.Now,
                            Patientaccountid = generatedId.ToString(),
                            Createduserid = user.Userid,
                            Ip = requestIpAddress,
                        };

                        _unitOfWork.RequestRepository.Add(request);
                        _unitOfWork.Save();

                        //Adding request in RequestClient Table
                        Requestclient requestclient = new()
                        {
                            Requestid = request.Requestid,
                            Firstname = userViewModel.FirstName,
                            Lastname = userViewModel.LastName,
                            Phonenumber = phoneNumber,
                            Email = userViewModel.Email,
                            Address = userViewModel.Street + " " + userViewModel.City + " " + userViewModel.State + ", " + userViewModel.ZipCode,
                            Street = userViewModel.Street,
                            City = userViewModel.City,
                            State = userViewModel.State,
                            Zipcode = userViewModel.ZipCode,
                            Notes = userViewModel.Symptom,
                            Ip = requestIpAddress,
                            Intdate = userViewModel.DOB.Value.Day,
                            Strmonth = userViewModel.DOB.Value.Month.ToString(),
                            Intyear = userViewModel.DOB.Value.Year,
                        };

                        _unitOfWork.RequestClientRepository.Add(requestclient);
                        _unitOfWork.Save();

                        //Adding File Data in RequestWiseFile Table
                        if (userViewModel.File != null)
                        {
                            InsertRequestWiseFile(userViewModel.File);

                            Requestwisefile requestwisefile = new()
                            {
                                Requestid = request.Requestid,
                                Createddate = DateTime.Now,
                                Ip = requestIpAddress,
                                Filename = userViewModel.File.FileName,
                            };

                            _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                            _unitOfWork.Save();
                        }



                        TempData["success"] = "Request Created Successfully.";
                        return RedirectToAction("Login");

                    }
                    else
                    {
                        TempData["error"] = "Password cannot be empty.";
                        return View("Request/PatientRequest");
                    }
                }
                else
                {
                    User user;

                    // Fetching Registered User
                    user = _unitOfWork.UserRepository.GetUserWithEmail(userViewModel.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Patient,
                        Userid = user.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = userViewModel.FirstName,
                        Lastname = userViewModel.LastName,
                        Phonenumber = phoneNumber,
                        Email = userViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = user.Aspnetuserid,
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = userViewModel.FirstName,
                        Lastname = userViewModel.LastName,
                        Phonenumber = phoneNumber,
                        Email = userViewModel.Email,
                        Address = userViewModel.Street + " " + userViewModel.City + " " + userViewModel.State + ", " + userViewModel.ZipCode,
                        Street = userViewModel.Street,
                        City = userViewModel.City,
                        State = userViewModel.State,
                        Zipcode = userViewModel.ZipCode,
                        Notes = userViewModel.Symptom,
                        Ip = requestIpAddress,
                        Intdate = userViewModel.DOB.Value.Day,
                        Strmonth = userViewModel.DOB.Value.Month.ToString(),
                        Intyear = userViewModel.DOB.Value.Year,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    //Adding File Data in RequestWiseFile Table
                    if (userViewModel.File != null)
                    {
                        InsertRequestWiseFile(userViewModel.File);

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = userViewModel.File.FileName,
                        };

                        _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                        _unitOfWork.Save();
                    }


                    TempData["success"] = "Request Created Successfully.";
                    return RedirectToAction("Login");
                }

            }

            return View("Request/PatientRequest");

        }

        public void InsertRequestWiseFile(IFormFile document)
        {
            string path = _environment.WebRootPath + "/document";
            string fileName = document.FileName;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            document.CopyTo(stream);
        }

        public static string GenerateConfirmationNumber(User user)
        {
            string confirmationNumber = "AD" + user.Createddate.Date.ToString("D2") + user.Createddate.Month.ToString("D2") + user.Lastname.Substring(0, 2).ToUpper() + user.Firstname.Substring(0, 2).ToUpper() + "0001";
            return confirmationNumber;
        }

        public static string GetRequestIP()
        {
            string ip = "127.0.0.1";
            return ip;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}