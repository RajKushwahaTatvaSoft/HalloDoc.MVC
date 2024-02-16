using Microsoft.AspNetCore.Mvc;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Data_Layer.DataContext;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;


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

        public PatientController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            string id = HttpContext.Session.GetString("userEmail");

            User? user = _context.Users.FirstOrDefault(u => u.Email == id);

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

        public IActionResult ViewDocument(int? id)
        {
            List<Requestwisefile> files = _context.Requestwisefiles.Where(reqFile => reqFile.Requestid == id).ToList();
            
            ViewDocumentViewModel viewDocumentVM = new ViewDocumentViewModel();
            viewDocumentVM.requestwisefiles = files;
            return View("Dashboard/ViewDocument",viewDocumentVM);
        }

        public IActionResult Profile(int? id)
        {
            User? user = _context.Users.FirstOrDefault(u => u.Userid == id);

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
            return View("Error");
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
            TempData["loginUserId"] = dbUser.Userid;
            return RedirectToAction("Dashboard");
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
                    HttpContext.Session.SetString("userEmail", patientUser.Email);
                    return RedirectToAction("Dashboard");
                }

            }
            TempData["error"] = "Invalid Username or Password";

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
            string path = _environment.WebRootPath;
            string filePath = "document/" + document.FileName;
            string fullPath = Path.Combine(path, filePath);

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
                string requestIpAddress = GetRequestIP();

                //either new will be created or existing one will be fetched
                User user = null;

                if (userViewModel.Password != null)
                {
                    Guid generatedId = Guid.NewGuid();
                    string phoneNumber = "+" + userViewModel.Countrycode + '-' + userViewModel.Phone;

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
                        Requesttypeid = 2,
                        Userid = user.Userid,
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
                        Address = userViewModel.Street,
                        City = userViewModel.City,
                        State = userViewModel.State,
                        Zipcode = userViewModel.ZipCode,
                        Notes = userViewModel.Symptom,
                        Ip = requestIpAddress,
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
                    // Fetching Registered User
                    user = _context.Users.FirstOrDefault(u => u.Email == userViewModel.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = 2,
                        Userid = user.Userid,
                        Firstname = userViewModel.FirstName,
                        Lastname = userViewModel.LastName,
                        Phonenumber = userViewModel.Phone,
                        Email = userViewModel.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
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
                        Phonenumber = userViewModel.Phone,
                        Email = userViewModel.Email,
                        Address = userViewModel.Street,
                        City = userViewModel.City,
                        State = userViewModel.State,
                        Zipcode = userViewModel.ZipCode,
                        Notes = userViewModel.Symptom,
                        Ip = requestIpAddress,
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
                TempData["loginUserId"] = user.Userid;
                return RedirectToAction("Dashboard");
            }

            return View();

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
                string requestIpAddress = GetRequestIP();

                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Firstname = friendViewModel.FirstName,
                    Lastname = friendViewModel.LastName,
                    Phonenumber = friendViewModel.Phone,
                    Email = friendViewModel.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                Requestclient reqClient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = friendViewModel.patientDetails.FirstName,
                    Lastname = friendViewModel.patientDetails.LastName,
                    Phonenumber = friendViewModel.patientDetails.Phone,
                    Notes = friendViewModel.patientDetails.Symptom,
                    Email = friendViewModel.patientDetails.Email,
                    Street = friendViewModel.patientDetails.Street,
                    City = friendViewModel.patientDetails.City,
                    State = friendViewModel.patientDetails.State,
                    Zipcode = friendViewModel.patientDetails.ZipCode,
                    Ip = requestIpAddress,
                };


                _context.Requestclients.Add(reqClient);
                _context.SaveChanges();
                if (friendViewModel.file != null)
                {
                    Requestwisefile reqWiseFile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = friendViewModel.file.FileName,
                    };

                    _context.Requestwisefiles.Add(reqWiseFile);
                    _context.SaveChanges();
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
                string requestIpAddress = GetRequestIP();

                Request request = new()
                {
                    Requesttypeid = 4,
                    Firstname = conciergeViewModel.FirstName,
                    Lastname = conciergeViewModel.LastName,
                    Phonenumber = conciergeViewModel.Phone,
                    Email = conciergeViewModel.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };
                _context.Requests.Add(request);
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


                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = conciergeViewModel.patientDetails.FirstName,
                    Lastname = conciergeViewModel.patientDetails.LastName,
                    Phonenumber = conciergeViewModel.patientDetails.Phone,
                    Notes = conciergeViewModel.patientDetails.Symptom,
                    Email = conciergeViewModel.patientDetails.Email,
                    Street = conciergeViewModel.patientDetails.Street,
                    City = conciergeViewModel.patientDetails.City,
                    State = conciergeViewModel.patientDetails.State,
                    Zipcode = conciergeViewModel.patientDetails.ZipCode,
                    Ip = requestIpAddress,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();


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
                string requestIpAddress = GetRequestIP();

                Business business = new()
                {
                    Name = businessViewModel.BusinessOrPropertyName,
                    Phonenumber = businessViewModel.Phone,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Businesses.Add(business);
                _context.SaveChanges();

                Request request = new()
                {
                    Requesttypeid = 1,
                    Firstname = businessViewModel.FirstName,
                    Lastname = businessViewModel.LastName,
                    Phonenumber = businessViewModel.Phone,
                    Email = businessViewModel.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                Requestclient reqClient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = businessViewModel.patientDetails.FirstName,
                    Lastname = businessViewModel.patientDetails.LastName,
                    Phonenumber = businessViewModel.patientDetails.Phone,
                    Email = businessViewModel.patientDetails.Email,
                    Street = businessViewModel.patientDetails.Street,
                    City = businessViewModel.patientDetails.City,
                    State = businessViewModel.patientDetails.State,
                    Zipcode = businessViewModel.patientDetails.ZipCode,
                    Ip = requestIpAddress,
                };

                _context.Requestclients.Add(reqClient);
                _context.SaveChanges();


                Requestbusiness reqBusiness = new()
                {
                    Requestid = request.Requestid,
                    Businessid = business.Id,
                };

                _context.Requestbusinesses.Add(reqBusiness);
                _context.SaveChanges();

                TempData["success"] = "Request Added Successfully.";
                return View("Index");

            }
            return View("Request/BusinessRequest");
        }
    }
}
