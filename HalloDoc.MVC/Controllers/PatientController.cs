using Microsoft.AspNetCore.Mvc;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Data_Layer.DataContext;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;


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
            return View();
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

                foreach (var aspnetuser in obj)
                {
                    if (aspnetuser.Username == loginUser.Username && aspnetuser.Passwordhash == loginUser.Passwordhash)
                    {
                        return View("Index");
                    }
                }
            }

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

                if (userViewModel.Password != null)
                {
                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = userViewModel.FirstName!,
                        Passwordhash = GetSHA256Hash(userViewModel.Password),
                        Email = userViewModel.Email,
                        Phonenumber = userViewModel.Phone,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Aspnetusers.Add(aspnetuser);
                    _context.SaveChanges();


                    // Creating Patient in User Table
                    User user = new()
                    {
                        Aspnetuserid = generatedId.ToString(),
                        Firstname = userViewModel.FirstName,
                        Lastname = userViewModel.LastName,
                        Email = userViewModel.Email,
                        Mobile = userViewModel.Phone,
                        Street = userViewModel.Street,
                        City = userViewModel.City,
                        State = userViewModel.State,
                        Zipcode = userViewModel.ZipCode,
                        Createddate = DateTime.Now,
                        Createdby = generatedId.ToString(),
                        Ip = requestIpAddress,
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
                        Phonenumber = userViewModel.Phone,
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
                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();

                }
                else
                {
                    // Fetching Registered User
                    User user = _context.Users.FirstOrDefault(u => u.Email == userViewModel.Email);

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
                    //Requestwisefile requestwisefile = new()
                    //{
                    //    Requestid = request.Requestid,
                    //    Createddate = DateTime.Now,
                    //    Ip = requestIpAddress,
                    //};

                    //_context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();

                }

                return View("Dashboard");
            }

            return View();

        }

        [HttpPost]
        public JsonResult PatientCheckEmail(string email)
        {
            bool emailExists = _context.Users.Any(u => u.Email == email);
            return Json(new { exists = emailExists });
        }

        public string GetSHA256Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8); // divide by 8 to convert bits to bytes

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashed;
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
                    Requesttypeid = (int) RequestType.Family,
                    Firstname = friendViewModel.FirstName,
                    Lastname = friendViewModel.LastName,
                    Phonenumber = friendViewModel.Phone,
                    Email = friendViewModel.Email,
                    Status = (short) RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                Requestclient reqClient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = friendViewModel.patientDetails.FirstName,
                    Lastname    = friendViewModel.patientDetails.LastName,
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

                Requestwisefile reqWiseFile = new()
                {
                    Requestid = request.Requestid,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                    Filename = friendViewModel.patientDetails.FilePath,
                };

                _context.Requestwisefiles.Add(reqWiseFile);
                _context.SaveChanges();

                return View("Dashboard");

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


                return View("Dashboard");
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

                return View("Dashboard");

            }
            return View("Request/BusinessRequest");
        }
    }
}
