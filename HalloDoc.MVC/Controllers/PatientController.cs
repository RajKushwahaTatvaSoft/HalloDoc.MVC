﻿using Microsoft.AspNetCore.Mvc;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Data_Layer.DataContext;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Mail;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using System.Security.Policy;
using System;

namespace HalloDoc.MVC.Controllers
{
    public enum RequestStatus
    {
        Unassigned = 1,
        Accepted = 2,
        Cancelled = 3,
        Reserving = 4,
        MDEnRoute = 5,
        MDOnSite = 6
    }
    public enum RequestType
    {
        Business = 1,
        Patient = 2,
        Family = 3,
        Concierge = 4
    }
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;

        public PatientController(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration config)
        {
            _context = context;
            _environment = environment;
            _config = config;
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
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null)
            {
                return View("Error");
            }

            User? user = _context.Users.FirstOrDefault(u => u.Userid == userId);

            if (user != null)
            {
                PatientDashboardViewModel dashboardVM = new PatientDashboardViewModel();
                dashboardVM.UserId = user.Userid;
                dashboardVM.UserName = user.Firstname + " " + user.Lastname;
                dashboardVM.Requests = _context.Requests.Where(req => req.Userid == user.Userid).ToList();
                List<int> fileCounts = new List<int>();
                foreach (var request in dashboardVM.Requests)
                {
                    int count = _context.Requestwisefiles.Count(reqFile => reqFile.Requestid == request.Requestid);
                    fileCounts.Add(count);
                }
                dashboardVM.DocumentCount = fileCounts;
                return View("Dashboard/Dashboard", dashboardVM);
            }

            return View("Error");
        }

        public IActionResult RequestForMe()
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null)
            {
                return View("Error");
            }

            User user = _context.Users.FirstOrDefault(u => u.Userid == userId);

            string dobDate = user.Intyear + "-" + user.Strmonth + "-" + user.Intdate;

            MeRequestViewModel model = new()
            {
                UserId = user.Userid,
                FirstName = user.Firstname,
                LastName = user.Lastname,
                DOB = DateTime.Parse(dobDate),
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

            User user = _context.Users.FirstOrDefault(u => u.Userid == userId);
            string requestIpAddress = GetRequestIP();
            if (ModelState.IsValid)
            {
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

                _context.Requests.Add(request);
                _context.SaveChanges();

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

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();

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

                    _context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();
                }


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

            User user = _context.Users.FirstOrDefault(u => u.Userid == userId);

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
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null)
            {
                return View("Error");
            }

            User relationUser = _context.Users.FirstOrDefault(u => u.Userid == userId);

            if (ModelState.IsValid)
            {
                string requestIpAddress = GetRequestIP();
                string phoneNumber = "+" + srvm.patientDetails.Countrycode + '-' + srvm.patientDetails.Phone;
                bool isUserExists = _context.Aspnetusers.Any(u => u.Email == srvm.patientDetails.Email);

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

                    _context.Aspnetusers.Add(aspnetuser);
                    _context.SaveChanges();

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
                        Intdate = srvm.patientDetails.DOB.Day,
                        Strmonth = srvm.patientDetails.DOB.Month.ToString(),
                        Intyear = srvm.patientDetails.DOB.Year,
                    };

                    _context.Users.Add(user);
                    _context.SaveChanges();

                    SendEmailForCreateAccount(srvm.patientDetails.Email);

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

                    _context.Requests.Add(request);
                    _context.SaveChanges();

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

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();

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

                        _context.Requestwisefiles.Add(requestwisefile);
                        _context.SaveChanges();
                    }
                }
                else
                {
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

                    _context.Requests.Add(request);
                    _context.SaveChanges();

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

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();

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

                        _context.Requestwisefiles.Add(requestwisefile);
                        _context.SaveChanges();
                    }

                }



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
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var email = jwtToken.Claims.First(x => x.Type == "email").Value;

                ForgotPasswordViewModel fpvm = new()
                {
                    Email = email,
                };

                return CreateAccountGet(fpvm);
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
            Aspnetuser user = _context.Aspnetusers.FirstOrDefault(u => u.Email == fpvm.Email);

            if (user == null)
            {
                return NotFound("Error");
            }

            if (ModelState.IsValid)
            {
                string passHash = GenerateSHA256(fpvm.Password);
                user.Passwordhash = passHash;
                user.Modifieddate = DateTime.Now;
                _context.Update(user);
                _context.SaveChanges();

                TempData["success"] = "Account Successfully Created.";
                return RedirectToAction("Login");

            }
            return View();
        }

        public void SendEmailForCreateAccount(string email)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim("email", email) }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    Issuer = _config["Jwt:Issuer"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                var createLink = Url.Action("CreateAccount", "Patient", new { token = jwtToken }, Request.Scheme);

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

        public IActionResult ViewDocument(int requestId)
        {
            int? userId = HttpContext.Session.GetInt32("userId");

            if (userId == null || requestId == null)
            {
                return View("Error");
            }

            User user = _context.Users.FirstOrDefault(u => u.Userid == userId);
            Request request = _context.Requests.FirstOrDefault(req => req.Requestid == requestId);

            List<Requestwisefile> files = _context.Requestwisefiles.Where(reqFile => reqFile.Requestid == requestId).ToList();

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
                _context.Requestwisefiles.Add(requestwisefile);
                _context.SaveChanges();

                viewDocumentVM.File = null;

            }

            return ViewDocument(viewDocumentVM.RequestId);
        }
        public string GenerateConfirmationNumber(User user)
        {
            string confirmationNumber = "AD" + user.Createddate.Date.ToString("D2") + user.Createddate.Month.ToString("D2") + user.Lastname.Substring(0, 2).ToUpper() + user.Firstname.Substring(0, 2).ToUpper() + "0001";
            return confirmationNumber;
        }
        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("userId");
            User? user = _context.Users.FirstOrDefault(u => u.Userid == userId);

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

            User dbUser = _context.Users.FirstOrDefault(u => u.Userid == pm.UserId);
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

            _context.Update(dbUser);
            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        public async Task<IActionResult> DownloadAllFiles(int requestId)
        {
            try
            {
                // Fetch all document details for the given request:
                var documentDetails = _context.Requestwisefiles.Where(m => m.Requestid == requestId).ToList();

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
                var obj = _context.Aspnetusers.ToList();
                var passHash = GenerateSHA256(loginUser.Passwordhash);
                Aspnetuser user = _context.Aspnetusers.FirstOrDefault(aspnetuser => aspnetuser.Username == loginUser.Username && aspnetuser.Passwordhash == passHash);

                if (user != null)
                {
                    User patientUser = _context.Users.FirstOrDefault(u => u.Aspnetuserid == user.Id);
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

        // email token isdeleted createddate aspnetuserid expirydate
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var email = jwtToken.Claims.First(x => x.Type == "email").Value;

                ForgotPasswordViewModel fpvm = new()
                {
                    Email = email,
                };

                return ResetPasswordGet(fpvm);
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult ResetPassword(ForgotPasswordViewModel fpvm)
        {
            Aspnetuser user = _context.Aspnetusers.FirstOrDefault(u => u.Email == fpvm.Email);

            if (user == null)
            {
                return NotFound("Error");
            }

            if (ModelState.IsValid)
            {
                string passHash = GenerateSHA256(fpvm.Password);
                user.Passwordhash = passHash;
                user.Modifieddate = DateTime.Now;
                _context.Update(user);
                _context.SaveChanges();

                TempData["success"] = "Password Reset Successful";
                return RedirectToAction("Login");

            }
            return View();
        }


        public IActionResult ForgetPassword()
        {
            return View("Authentication/ForgetPassword");
        }


        public IActionResult SendMailForForgetPassword(string email, string onSucessAction, string onSucessController, string emailAction, string emailController, string onFailedAction, string onFailedController)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim("email", email) }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    Issuer = _config["Jwt:Issuer"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                var resetLink = Url.Action(emailAction, emailController, new { token = jwtToken }, Request.Scheme);

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
                TempData["success"] = "Please check " + email + " for reset password link.";
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
                bool isUserExists = _context.Aspnetusers.Any(u => u.Email == forPasVM.Email);
                if (isUserExists)
                {
                    return SendMailForForgetPassword(forPasVM.Email, "ForgetPassword", "Patient", "ResetPassword", "Patient", "ForgetPassword", "Patient");
                }
            }
            TempData["error"] = "User doesn't exist.";
            return View("Authentication/ForgetPassword");

        }

        public IActionResult SubmitRequest()
        {
            return View("Request/SubmitRequest");
        }

        //GET
        public IActionResult PatientRequest()
        {
            return View("Request/PatientRequest");
        }

        public string GetRequestIP()
        {
            return "127.0.0.1";
        }

        public void InsertRequestWiseFile(IFormFile document)
        {
            string path = _environment.WebRootPath + "/document";
            string fileName= document.FileName;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            document.CopyTo(stream);
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientRequest(PatientRequestViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {
                User user = null;
                string requestIpAddress = GetRequestIP();
                string phoneNumber = "+" + userViewModel.Countrycode + '-' + userViewModel.Phone;

                bool isUserExists = _context.Aspnetusers.Any(u => u.Email == userViewModel.Email);


                if (!isUserExists)
                {

                    if (userViewModel.Password != null)
                    {
                        Guid generatedId = Guid.NewGuid();

                        // Creating Patient in Aspnetusers Table
                        Aspnetuser aspnetuser = new()
                        {
                            Id = generatedId.ToString(),
                            Username = userViewModel.Email!,
                            Passwordhash = GenerateSHA256(userViewModel.Password),
                            Email = userViewModel.Email,
                            Phonenumber = phoneNumber,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                        };

                        _context.Aspnetusers.Add(aspnetuser);
                        _context.SaveChanges();


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
                            Intdate = userViewModel.DOB.Day,
                            Strmonth = userViewModel.DOB.Month.ToString(),
                            Intyear = userViewModel.DOB.Year,
                        };

                        _context.Users.Add(user);
                        _context.SaveChanges();

                        // Adding request in Request Table
                        Request request = new()
                        {
                            Requesttypeid = (int) RequestType.Patient,
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

                        _context.Requests.Add(request);
                        _context.SaveChanges();

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
                            Intdate = userViewModel.DOB.Day,
                            Strmonth = userViewModel.DOB.Month.ToString(),
                            Intyear = userViewModel.DOB.Year,
                        };

                        _context.Requestclients.Add(requestclient);
                        _context.SaveChanges();

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

                            _context.Requestwisefiles.Add(requestwisefile);
                            _context.SaveChanges();
                        }

                    }
                    else
                    {
                        TempData["error"] = "Password cannot be empty";
                        return View("Request/PatientRequest");
                    }
                }
                else
                {
                    // Fetching Registered User
                    user = _context.Users.FirstOrDefault(u => u.Email == userViewModel.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int) RequestType.Patient,
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

                    _context.Requests.Add(request);
                    _context.SaveChanges();

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
                        Intdate = userViewModel.DOB.Day,
                        Strmonth = userViewModel.DOB.Month.ToString(),
                        Intyear = userViewModel.DOB.Year,
                    };

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();


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

                        _context.Requestwisefiles.Add(requestwisefile);
                        _context.SaveChanges();
                    }

                }
                return RedirectToAction("Login");
            }

            return View("Request/PatientRequest");

        }

        [HttpPost]
        public JsonResult PatientCheckEmail(string email)
        {
            bool emailExists = _context.Users.Any(u => u.Email == email);
            return Json(new { exists = emailExists });
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


        public IActionResult FamilyFriendRequest()
        {
            return View("Request/FamilyFriendRequest");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FamilyFriendRequest(FamilyFriendRequestViewModel friendViewModel)
        {
            if (ModelState.IsValid)
            {
                User user = null;
                string requestIpAddress = GetRequestIP();
                string familyNumber = "+" + friendViewModel.Countrycode + '-' + friendViewModel.Phone;
                string patientNumber = "+" + friendViewModel.patientDetails.Countrycode + '-' + friendViewModel.patientDetails.Phone;

                bool isUserExists = _context.Aspnetusers.Any(u => u.Email == friendViewModel.patientDetails.Email);

                if (!isUserExists)
                {
                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = friendViewModel.patientDetails.Email,
                        Email = friendViewModel.patientDetails.Email,
                        Phonenumber = patientNumber,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Aspnetusers.Add(aspnetuser);
                    _context.SaveChanges();


                    // Creating Patient in User Table
                    user = new()
                    {
                        Aspnetuserid = generatedId.ToString(),
                        Firstname = friendViewModel.patientDetails.FirstName,
                        Lastname = friendViewModel.patientDetails.LastName,
                        Email = friendViewModel.patientDetails.Email,
                        Mobile = patientNumber,
                        Street = friendViewModel.patientDetails.Street,
                        City = friendViewModel.patientDetails.City,
                        State = friendViewModel.patientDetails.State,
                        Zipcode = friendViewModel.patientDetails.ZipCode,
                        Createddate = DateTime.Now,
                        Createdby = generatedId.ToString(),
                        Ip = requestIpAddress,
                        Intdate = friendViewModel.patientDetails.DOB.Day,
                        Strmonth = friendViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = friendViewModel.patientDetails.DOB.Year,
                    };

                    _context.Users.Add(user);
                    _context.SaveChanges();

                    SendEmailForCreateAccount(friendViewModel.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int) RequestType.Family,
                        Userid = user.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = friendViewModel.FirstName,
                        Lastname = friendViewModel.LastName,
                        Phonenumber = familyNumber,
                        Email = friendViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = generatedId.ToString(),
                        Createduserid = user.Userid,
                        Relationname = friendViewModel.Relation,
                        Ip = requestIpAddress,
                    };

                    _context.Requests.Add(request);
                    _context.SaveChanges();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = friendViewModel.patientDetails.FirstName,
                        Lastname = friendViewModel.patientDetails.LastName,
                        Phonenumber = patientNumber,
                        Email = friendViewModel.patientDetails.Email,
                        Address = friendViewModel.patientDetails.Street + " " + friendViewModel.patientDetails.City + " " + friendViewModel.patientDetails.State + ", " + friendViewModel.patientDetails.ZipCode,
                        Street = friendViewModel.patientDetails.Street,
                        City = friendViewModel.patientDetails.City,
                        State = friendViewModel.patientDetails.State,
                        Zipcode = friendViewModel.patientDetails.ZipCode,
                        Notes = friendViewModel.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = friendViewModel.patientDetails.DOB.Day,
                        Strmonth = friendViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = friendViewModel.patientDetails.DOB.Year,
                    };

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();

                    //Adding File Data in RequestWiseFile Table
                    if (friendViewModel.patientDetails.File != null)
                    {
                        InsertRequestWiseFile(friendViewModel.patientDetails.File);

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = friendViewModel.patientDetails.File.FileName,
                        };

                        _context.Requestwisefiles.Add(requestwisefile);
                        _context.SaveChanges();
                    }
                }
                else
                {

                    // Fetching Registered User
                    user = _context.Users.FirstOrDefault(u => u.Email == friendViewModel.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Family,
                        Userid = user.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = friendViewModel.FirstName,
                        Lastname = friendViewModel.LastName,
                        Phonenumber = familyNumber,
                        Email = friendViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = user.Aspnetuserid,
                        Createduserid = user.Userid,
                        Relationname = friendViewModel.Relation,
                        Ip = requestIpAddress,
                    };

                    _context.Requests.Add(request);
                    _context.SaveChanges();

                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = friendViewModel.patientDetails.FirstName,
                        Lastname = friendViewModel.patientDetails.LastName,
                        Phonenumber = patientNumber,
                        Email = friendViewModel.patientDetails.Email,
                        Address = friendViewModel.patientDetails.Street + " " + friendViewModel.patientDetails.City + " " + friendViewModel.patientDetails.State + ", " + friendViewModel.patientDetails.ZipCode,
                        Street = friendViewModel.patientDetails.Street,
                        City = friendViewModel.patientDetails.City,
                        State = friendViewModel.patientDetails.State,
                        Zipcode = friendViewModel.patientDetails.ZipCode,
                        Notes = friendViewModel.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = friendViewModel.patientDetails.DOB.Day,
                        Strmonth = friendViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = friendViewModel.patientDetails.DOB.Year,
                    };

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();

                    if (friendViewModel.patientDetails.File != null)
                    {

                        InsertRequestWiseFile(friendViewModel.patientDetails.File);

                        Requestwisefile reqWiseFile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = friendViewModel.patientDetails.File.FileName,
                        };

                        _context.Requestwisefiles.Add(reqWiseFile);
                        _context.SaveChanges();
                    }

                }

                TempData["success"] = "Request Added Successfully.";
                return View("Index");

            }
            return View("Request/FamilyFriendRequest");
        }

        public IActionResult ConciergeRequest()
        {
            return View("Request/ConciergeRequest");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConciergeRequest(ConciergeRequestViewModel conciergeViewModel)
        {
            if (ModelState.IsValid)
            {
                User user = null;
                string requestIpAddress = GetRequestIP();
                string conciergeNumber = "+" + conciergeViewModel.Countrycode + '-' + conciergeViewModel.Phone;
                string patientNumber = "+" + conciergeViewModel.patientDetails.Countrycode + '-' + conciergeViewModel.patientDetails.Phone;

                bool isUserExists = _context.Aspnetusers.Any(u => u.Email == conciergeViewModel.patientDetails.Email);

                if (!isUserExists)
                {
                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = conciergeViewModel.patientDetails.Email,
                        Email = conciergeViewModel.patientDetails.Email,
                        Phonenumber = patientNumber,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Aspnetusers.Add(aspnetuser);
                    _context.SaveChanges();


                    // Creating Patient in User Table
                    user = new()
                    {
                        Aspnetuserid = generatedId.ToString(),
                        Firstname = conciergeViewModel.patientDetails.FirstName,
                        Lastname = conciergeViewModel.patientDetails.LastName,
                        Email = conciergeViewModel.patientDetails.Email,
                        Mobile = patientNumber,
                        Street = conciergeViewModel.patientDetails.Street,
                        City = conciergeViewModel.patientDetails.City,
                        State = conciergeViewModel.patientDetails.State,
                        Zipcode = conciergeViewModel.patientDetails.ZipCode,
                        Createddate = DateTime.Now,
                        Createdby = generatedId.ToString(),
                        Ip = requestIpAddress,
                        Intdate = conciergeViewModel.patientDetails.DOB.Day,
                        Strmonth = conciergeViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = conciergeViewModel.patientDetails.DOB.Year,
                    };

                    _context.Users.Add(user);
                    _context.SaveChanges();

                    SendEmailForCreateAccount(conciergeViewModel.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Concierge,
                        Userid = user.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = conciergeViewModel.FirstName,
                        Lastname = conciergeViewModel.LastName,
                        Phonenumber = conciergeNumber,
                        Email = conciergeViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = generatedId.ToString(),
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _context.Requests.Add(request);
                    _context.SaveChanges();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = conciergeViewModel.patientDetails.FirstName,
                        Lastname = conciergeViewModel.patientDetails.LastName,
                        Phonenumber = patientNumber,
                        Email = conciergeViewModel.patientDetails.Email,
                        Address = conciergeViewModel.patientDetails.Street + " " + conciergeViewModel.patientDetails.City + " " + conciergeViewModel.patientDetails.State + ", " + conciergeViewModel.patientDetails.ZipCode,
                        Street = conciergeViewModel.patientDetails.Street,
                        City = conciergeViewModel.patientDetails.City,
                        State = conciergeViewModel.patientDetails.State,
                        Zipcode = conciergeViewModel.patientDetails.ZipCode,
                        Notes = conciergeViewModel.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = conciergeViewModel.patientDetails.DOB.Day,
                        Strmonth = conciergeViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = conciergeViewModel.patientDetails.DOB.Year,
                    };

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();


                    Concierge concierge = new()
                    {
                        Conciergename = conciergeViewModel.FirstName,
                        Address = conciergeViewModel.HotelOrPropertyName,
                        Street = conciergeViewModel.Street,
                        City = conciergeViewModel.City,
                        State = conciergeViewModel.State,
                        Zipcode = conciergeViewModel.ZipCode,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Concierges.Add(concierge);
                    _context.SaveChanges();

                    Requestconcierge reqConcierge = new()
                    {
                        Requestid = request.Requestid,
                        Conciergeid = concierge.Conciergeid,
                        Ip = requestIpAddress,
                    };

                    _context.Requestconcierges.Add(reqConcierge);
                    _context.SaveChanges();

                }
                else
                {

                    // Fetching Registered User
                    user = _context.Users.FirstOrDefault(u => u.Email == conciergeViewModel.patientDetails.Email);


                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Concierge,
                        Userid = user.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = conciergeViewModel.FirstName,
                        Lastname = conciergeViewModel.LastName,
                        Phonenumber = conciergeNumber,
                        Email = conciergeViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = user.Aspnetuserid,
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _context.Requests.Add(request);
                    _context.SaveChanges();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = conciergeViewModel.patientDetails.FirstName,
                        Lastname = conciergeViewModel.patientDetails.LastName,
                        Phonenumber = patientNumber,
                        Email = conciergeViewModel.patientDetails.Email,
                        Address = conciergeViewModel.patientDetails.Street + " " + conciergeViewModel.patientDetails.City + " " + conciergeViewModel.patientDetails.State + ", " + conciergeViewModel.patientDetails.ZipCode,
                        Street = conciergeViewModel.patientDetails.Street,
                        City = conciergeViewModel.patientDetails.City,
                        State = conciergeViewModel.patientDetails.State,
                        Zipcode = conciergeViewModel.patientDetails.ZipCode,
                        Notes = conciergeViewModel.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = conciergeViewModel.patientDetails.DOB.Day,
                        Strmonth = conciergeViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = conciergeViewModel.patientDetails.DOB.Year,
                    };

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();


                    Concierge concierge = new()
                    {
                        Conciergename = conciergeViewModel.FirstName,
                        Address = conciergeViewModel.HotelOrPropertyName,
                        Street = conciergeViewModel.Street,
                        City = conciergeViewModel.City,
                        State = conciergeViewModel.State,
                        Zipcode = conciergeViewModel.ZipCode,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Concierges.Add(concierge);
                    _context.SaveChanges();

                    Requestconcierge reqConcierge = new()
                    {
                        Requestid = request.Requestid,
                        Conciergeid = concierge.Conciergeid,
                        Ip = requestIpAddress,
                    };

                    _context.Requestconcierges.Add(reqConcierge);
                    _context.SaveChanges();

                }


                TempData["success"] = "Request Added Successfully.";
                return View("Index");
            }
            return View("Request/ConciergeRequest");
        }

        public IActionResult BusinessRequest()
        {
            return View("Request/BusinessRequest");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BusinessRequest(BusinessRequestViewModel businessViewModel)
        {
            if (ModelState.IsValid)
            {

                User user = null;
                string requestIpAddress = GetRequestIP();
                string businessNumber = "+" + businessViewModel.Countrycode + '-' + businessViewModel.Phone;
                string patientNumber = "+" + businessViewModel.patientDetails.Countrycode + '-' + businessViewModel.patientDetails.Phone;

                bool isUserExists = _context.Aspnetusers.Any(u => u.Email == businessViewModel.patientDetails.Email);

                if (!isUserExists)
                {

                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = businessViewModel.patientDetails.Email,
                        Email = businessViewModel.patientDetails.Email,
                        Phonenumber = patientNumber,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Aspnetusers.Add(aspnetuser);
                    _context.SaveChanges();


                    // Creating Patient in User Table
                    user = new()
                    {
                        Aspnetuserid = generatedId.ToString(),
                        Firstname = businessViewModel.patientDetails.FirstName,
                        Lastname = businessViewModel.patientDetails.LastName,
                        Email = businessViewModel.patientDetails.Email,
                        Mobile = patientNumber,
                        Street = businessViewModel.patientDetails.Street,
                        City = businessViewModel.patientDetails.City,
                        State = businessViewModel.patientDetails.State,
                        Zipcode = businessViewModel.patientDetails.ZipCode,
                        Createddate = DateTime.Now,
                        Createdby = generatedId.ToString(),
                        Ip = requestIpAddress,
                        Intdate = businessViewModel.patientDetails.DOB.Day,
                        Strmonth = businessViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = businessViewModel.patientDetails.DOB.Year,
                    };

                    _context.Users.Add(user);
                    _context.SaveChanges();

                    SendEmailForCreateAccount(businessViewModel.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Concierge,
                        Userid = user.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = businessViewModel.FirstName,
                        Lastname = businessViewModel.LastName,
                        Phonenumber = businessNumber,
                        Email = businessViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Casenumber = businessViewModel.CaseNumber,
                        Patientaccountid = generatedId.ToString(),
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _context.Requests.Add(request);
                    _context.SaveChanges();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = businessViewModel.patientDetails.FirstName,
                        Lastname = businessViewModel.patientDetails.LastName,
                        Phonenumber = patientNumber,
                        Email = businessViewModel.patientDetails.Email,
                        Address = businessViewModel.patientDetails.Street + " " + businessViewModel.patientDetails.City + " " + businessViewModel.patientDetails.State + ", " + businessViewModel.patientDetails.ZipCode,
                        Street = businessViewModel.patientDetails.Street,
                        City = businessViewModel.patientDetails.City,
                        State = businessViewModel.patientDetails.State,
                        Zipcode = businessViewModel.patientDetails.ZipCode,
                        Notes = businessViewModel.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = businessViewModel.patientDetails.DOB.Day,
                        Strmonth = businessViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = businessViewModel.patientDetails.DOB.Year,
                    };

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();


                    Business business = new()
                    {
                        Name = businessViewModel.BusinessOrPropertyName,
                        Phonenumber = businessViewModel.Phone,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Businesses.Add(business);
                    _context.SaveChanges();

                    Requestbusiness reqBusiness = new()
                    {
                        Requestid = request.Requestid,
                        Businessid = business.Id,
                    };

                    _context.Requestbusinesses.Add(reqBusiness);
                    _context.SaveChanges();

                }
                else
                {

                    // Fetching Registered User
                    user = _context.Users.FirstOrDefault(u => u.Email == businessViewModel.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Concierge,
                        Userid = user.Userid,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Firstname = businessViewModel.FirstName,
                        Lastname = businessViewModel.LastName,
                        Phonenumber = businessNumber,
                        Email = businessViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Casenumber = businessViewModel.CaseNumber,
                        Patientaccountid = user.Aspnetuserid,
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _context.Requests.Add(request);
                    _context.SaveChanges();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = businessViewModel.patientDetails.FirstName,
                        Lastname = businessViewModel.patientDetails.LastName,
                        Phonenumber = patientNumber,
                        Email = businessViewModel.patientDetails.Email,
                        Address = businessViewModel.patientDetails.Street + " " + businessViewModel.patientDetails.City + " " + businessViewModel.patientDetails.State + ", " + businessViewModel.patientDetails.ZipCode,
                        Street = businessViewModel.patientDetails.Street,
                        City = businessViewModel.patientDetails.City,
                        State = businessViewModel.patientDetails.State,
                        Zipcode = businessViewModel.patientDetails.ZipCode,
                        Notes = businessViewModel.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = businessViewModel.patientDetails.DOB.Day,
                        Strmonth = businessViewModel.patientDetails.DOB.Month.ToString(),
                        Intyear = businessViewModel.patientDetails.DOB.Year,
                    };

                    _context.Requestclients.Add(requestclient);
                    _context.SaveChanges();


                    Business business = new()
                    {
                        Name = businessViewModel.BusinessOrPropertyName,
                        Phonenumber = businessViewModel.Phone,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Businesses.Add(business);
                    _context.SaveChanges();

                    Requestbusiness reqBusiness = new()
                    {
                        Requestid = request.Requestid,
                        Businessid = business.Id,
                    };

                    _context.Requestbusinesses.Add(reqBusiness);
                    _context.SaveChanges();

                }
                TempData["success"] = "Request Added Successfully.";
                return View("Index");

            }
            return View("Request/BusinessRequest");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
