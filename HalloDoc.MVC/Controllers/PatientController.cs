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
using HalloDoc.MVC.Services;
using Business_Layer.Utilities;

namespace HalloDoc.MVC.Controllers
{

    [CustomAuthorize((int)AllowRole.Patient)]
    public class PatientController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        private readonly IPatientDashboardRepository _dashboardRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PatientController(IWebHostEnvironment environment, IConfiguration config, IPatientDashboardRepository patientDashboardRepository, IUnitOfWork unitwork)
        {
            _environment = environment;
            _config = config;
            _dashboardRepo = patientDashboardRepository;
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

        [HttpPost]
        public async Task<IActionResult> FetchDashboardTable(int page)
        {
            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            int pageSize = 5;

            var pagedList = await _dashboardRepo.GetPatientRequestsAsync(userId, page, pageSize);

            return PartialView("Partial/DashboardTable",pagedList);
        }

        public IActionResult Dashboard()
        {

            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string userName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            if (userId == null)
            {
                return View("Error");
            }


            PatientDashboardViewModel model = new PatientDashboardViewModel()
            {
                UserId = userId,
                UserName = userName,
            };


            return View("Dashboard/Dashboard", model);
        }

        public IActionResult RequestForMe()
        {

            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);


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
                State = user.State,
                ZipCode = user.Zipcode,
                RegionId = user.Regionid,
                regions = _unitOfWork.RegionRepository.GetAll(),
            };


            return View("Dashboard/RequestForMe", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestForMe(MeRequestViewModel meRequestViewModel)
        {

            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (userId == null)
            {
                return View("Error");
            }

            if (ModelState.IsValid)
            {

                User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);
                string requestIpAddress = GetRequestIP();
                string phoneNumber = "+" + meRequestViewModel.Countrycode + '-' + meRequestViewModel.Phone;
                string state = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == meRequestViewModel.RegionId).Name;

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
                    Regionid = meRequestViewModel.RegionId,
                    State = state,
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

            meRequestViewModel.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Dashboard/RequestForMe", meRequestViewModel);
        }

        public IActionResult RequestForSomeoneElse()
        {
            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (userId == null)
            {
                return View("Error");
            }

            User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);

            SomeoneElseRequestViewModel model = new()
            {
                Username = user.Firstname + " " + user.Lastname,
                regions = _unitOfWork.RegionRepository.GetAll(),
            };

            return View("Dashboard/RequestForSomeoneElse", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestForSomeoneElse(SomeoneElseRequestViewModel srvm)
        {

            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (userId == null)
            {
                return View("Error");
            }

            if (ModelState.IsValid)
            {
                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(srvm.patientDetails.Email);

                User relationUser = _unitOfWork.UserRepository.GetUserWithID((int)userId);
                string requestIpAddress = GetRequestIP();
                string phoneNumber = "+" + srvm.patientDetails.Countrycode + '-' + srvm.patientDetails.Phone;
                string state = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == srvm.patientDetails.RegionId).Name;

                User user = null;

                if (!isUserExists)
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
                        Regionid = srvm.patientDetails.RegionId,
                        State = state,
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
                        Regionid = srvm.patientDetails.RegionId,
                        State = state,
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
                        Regionid = srvm.patientDetails.RegionId,
                        State = state,
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

                return RedirectToAction("Dashboard");

            }

            srvm.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Dashboard/RequestForSomeoneElse", srvm);
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

            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string userName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

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

            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
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

            return Redirect("/Guest/Login");
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

                    return View("Authentication/ResetPassword", fpvm);
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

        public string GenerateConfirmationNumber(User user)
        {
            string regionAbbr = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == user.Regionid).Abbreviation;

            DateTime todayStart = DateTime.Now.Date;
            int count = _unitOfWork.RequestRepository.Count(req => req.Createddate > todayStart);

            string confirmationNumber = regionAbbr + user.Createddate.Date.ToString("D2") + user.Createddate.Month.ToString("D2") + user.Lastname.Substring(0, 2).ToUpper() + user.Firstname.Substring(0, 2).ToUpper() + (count + 1).ToString("D4");
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