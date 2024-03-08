using Business_Layer.Helpers;
using Business_Layer.Interface;
using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace HalloDoc.MVC.Controllers
{
    public class GuestController : Controller
    {
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        public GuestController(IUnitOfWork unitOfWork, IJwtService jwt, IWebHostEnvironment environment, IConfiguration config)
        {
            _jwtService = jwt;
            _unitOfWork = unitOfWork;
            _environment = environment;
            _config = config;
        }

        public IActionResult Index()
        {
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


        public IActionResult SubmitRequest()
        {
            return View("Request/SubmitRequest");
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

                if (aspUser == null)
                {
                    TempData["error"] = "User doesn't exists";
                    return View("Patient/PatientLogin");
                }

                User patientUser = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                if (patientUser == null)
                {
                    TempData["error"] = "User doesn't exists";
                    return View("Patient/PatientLogin");
                }


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

            TempData["error"] = "Invalid Username or Password";
            return View("Patient/PatientLogin");

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
                var passHash = GenerateSHA256(loginUser.Passwordhash);
                Aspnetuser aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(aspnetuser => aspnetuser.Email == loginUser.Username && aspnetuser.Passwordhash == passHash);

                if (aspUser == null)
                {
                    TempData["error"] = "User doesn't exists";
                    return View();
                }

                Admin adminUser = _unitOfWork.AdminRepository.GetFirstOrDefault(u => u.Aspnetuserid == aspUser.Id);
                if (adminUser == null)
                {
                    TempData["error"] = "User doesn't exists";
                    return View();
                }

                SessionUser sessionUser = new SessionUser()
                {
                    UserId = adminUser.Adminid,
                    Email = adminUser.Email,
                    RoleId = (int)AllowRole.Admin,
                    UserName = adminUser.Firstname + (String.IsNullOrEmpty(adminUser.Lastname) ? "" : adminUser.Lastname),
                };

                TempData["success"] = "Admin Login Successful";
                HttpContext.Session.SetInt32("adminId", adminUser.Adminid);


                var jwtToken = _jwtService.GenerateJwtToken(sessionUser);
                Response.Cookies.Append("hallodoc", jwtToken);
                HttpContext.Session.SetInt32("adminId", adminUser.Adminid);

                return RedirectToAction("Dashboard", "Admin");



            }
            TempData["error"] = "Invalid Username or Password";

            return View();

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
            return View("Request/FamilyFriendRequest");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FamilyFriendRequest(FamilyFriendRequestViewModel friendViewModel)
        {
            if (ModelState.IsValid)
            {

                User user = null;
                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(friendViewModel.patientDetails.Email);
                string requestIpAddress = GetRequestIP();
                string familyNumber = "+" + friendViewModel.Countrycode + '-' + friendViewModel.Phone;
                string patientNumber = "+" + friendViewModel.patientDetails.Countrycode + '-' + friendViewModel.patientDetails.Phone;

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


                    _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                    _unitOfWork.Save();


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
                        Intdate = friendViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = friendViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = friendViewModel.patientDetails.DOB.Value.Year,
                    };


                    _unitOfWork.UserRepository.Add(user);
                    _unitOfWork.Save();

                    SendMailForCreateAccount(friendViewModel.patientDetails.Email);

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
                        Patientaccountid = generatedId.ToString(),
                        Createduserid = user.Userid,
                        Relationname = friendViewModel.Relation,
                        Ip = requestIpAddress,
                    };


                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();


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
                        Intdate = friendViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = friendViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = friendViewModel.patientDetails.DOB.Value.Year,
                    };



                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();


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

                        _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                        _unitOfWork.Save();
                    }

                }
                else
                {

                    // Fetching Registered User
                    user = _unitOfWork.UserRepository.GetUserWithEmail(friendViewModel.patientDetails.Email);

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



                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();



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
                        Intdate = friendViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = friendViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = friendViewModel.patientDetails.DOB.Value.Year,
                    };



                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();


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


                        _unitOfWork.RequestWiseFileRepository.Add(reqWiseFile);
                        _unitOfWork.Save();
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

                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(conciergeViewModel.patientDetails.Email);

                User user = null;
                string requestIpAddress = GetRequestIP();
                string conciergeNumber = "+" + conciergeViewModel.Countrycode + '-' + conciergeViewModel.Phone;
                string patientNumber = "+" + conciergeViewModel.patientDetails.Countrycode + '-' + conciergeViewModel.patientDetails.Phone;

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


                    _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                    _unitOfWork.Save();


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
                        Intdate = conciergeViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = conciergeViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = conciergeViewModel.patientDetails.DOB.Value.Year,
                    };

                    _unitOfWork.UserRepository.Add(user);
                    _unitOfWork.Save();




                    SendMailForCreateAccount(conciergeViewModel.patientDetails.Email);

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



                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();



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
                        Intdate = conciergeViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = conciergeViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = conciergeViewModel.patientDetails.DOB.Value.Year,
                    };


                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

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

                    _unitOfWork.ConciergeRepository.Add(concierge);
                    _unitOfWork.Save();

                    Requestconcierge reqConcierge = new()
                    {
                        Requestid = request.Requestid,
                        Conciergeid = concierge.Conciergeid,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestConciergeRepository.Add(reqConcierge);
                    _unitOfWork.Save();


                }
                else
                {

                    // Fetching Registered User
                    user = _unitOfWork.UserRepository.GetUserWithEmail(conciergeViewModel.patientDetails.Email);

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


                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();


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
                        Intdate = conciergeViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = conciergeViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = conciergeViewModel.patientDetails.DOB.Value.Year,
                    };


                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

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
                    _unitOfWork.ConciergeRepository.Add(concierge);
                    _unitOfWork.Save();



                    Requestconcierge reqConcierge = new()
                    {
                        Requestid = request.Requestid,
                        Conciergeid = concierge.Conciergeid,
                        Ip = requestIpAddress,
                    };


                    _unitOfWork.RequestConciergeRepository.Add(reqConcierge);
                    _unitOfWork.Save();


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

                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(businessViewModel.patientDetails.Email);

                User user = null;
                string requestIpAddress = GetRequestIP();
                string businessNumber = "+" + businessViewModel.Countrycode + '-' + businessViewModel.Phone;
                string patientNumber = "+" + businessViewModel.patientDetails.Countrycode + '-' + businessViewModel.patientDetails.Phone;

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

                    _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                    _unitOfWork.Save();

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
                        Intdate = businessViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = businessViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = businessViewModel.patientDetails.DOB.Value.Year,
                    };

                    _unitOfWork.UserRepository.Add(user);
                    _unitOfWork.Save();

                    SendMailForCreateAccount(businessViewModel.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Business,
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


                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();



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
                        Intdate = businessViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = businessViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = businessViewModel.patientDetails.DOB.Value.Year,
                    };


                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();


                    Business business = new()
                    {
                        Name = businessViewModel.BusinessOrPropertyName,
                        Phonenumber = businessViewModel.Phone,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.BusinessRepo.Add(business);
                    _unitOfWork.Save();

                    Requestbusiness reqBusiness = new()
                    {
                        Requestid = request.Requestid,
                        Businessid = business.Id,
                    };

                    _unitOfWork.RequestBusinessRepo.Add(reqBusiness);
                    _unitOfWork.Save();

                }
                else
                {

                    // Fetching Registered User
                    user = _unitOfWork.UserRepository.GetUserWithEmail(businessViewModel.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Business,
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

                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();

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
                        Intdate = businessViewModel.patientDetails.DOB.Value.Day,
                        Strmonth = businessViewModel.patientDetails.DOB.Value.Month.ToString(),
                        Intyear = businessViewModel.patientDetails.DOB.Value.Year,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    Business business = new()
                    {
                        Name = businessViewModel.BusinessOrPropertyName,
                        Phonenumber = businessViewModel.Phone,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.BusinessRepo.Add(business);
                    _unitOfWork.Save();

                    Requestbusiness reqBusiness = new()
                    {
                        Requestid = request.Requestid,
                        Businessid = business.Id,
                    };

                    _unitOfWork.RequestBusinessRepo.Add(reqBusiness);
                    _unitOfWork.Save();

                }

                TempData["success"] = "Request Added Successfully.";
                return View("Index");

            }
            return View("Request/BusinessRequest");
        }


        public static string GenerateConfirmationNumber(User user)
        {
            string confirmationNumber = "AD" + user.Createddate.Date.ToString("D2") + user.Createddate.Month.ToString("D2") + user.Lastname.Substring(0, 2).ToUpper() + user.Firstname.Substring(0, 2).ToUpper() + "0001";
            return confirmationNumber;
        }

    }
}
