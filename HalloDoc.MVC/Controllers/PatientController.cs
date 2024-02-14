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

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Dashboard()
        {
            int id = Convert.ToInt32(TempData["loginUserId"]);

            User? user = _context.Users.FirstOrDefault(u => u.Userid == id);

            if (user != null)
            {
                return View("Dashboard/Dashboard", user);
            }
            return View("Error");
        }

        public IActionResult ViewDocument(int? id)
        {
            User? user = _context.Users.FirstOrDefault(u => u.Userid == id);

            if (user != null)
            {
                return View("Dashboard/ViewDocument", user);
            }
            return View("Error");
        }

        public IActionResult Profile(int? id)
        {
            User? user = _context.Users.FirstOrDefault(u => u.Userid == id);

            if (user != null)
            {
                return View("Dashboard/Profile", user);
            }
            return View("Error");
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
                    TempData["loginUserId"] = patientUser.Userid;
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
                    if (userViewModel.file != null)
                    {

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = userViewModel.file.FileName,
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
                    if (userViewModel.file != null)
                    {

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = userViewModel.file.FileName,
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
