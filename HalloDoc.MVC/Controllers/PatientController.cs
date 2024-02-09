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
    public class PatientController : Controller
    {
        private readonly ApplicationDBContext _context;

        public PatientController(ApplicationDBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
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

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientRequest(PatientRequestViewModel userViewModel)
        {
            if (ModelState.IsValid)
            {

                Guid generatedId = Guid.NewGuid();

                // Creating Patient in Aspnetusers Table
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = userViewModel.FirstName!,
                    Passwordhash = GetSHA256Hash("123456"),
                    Email = userViewModel.Email,
                    Phonenumber = userViewModel.Phone,
                    Createddate = DateTime.Now
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
                    Createdby = generatedId.ToString()
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
                    Status = (short) RequestStatus.Unassigned ,
                    Createddate= DateTime.Now,
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
                    Notes = userViewModel.Symptom
                };

                _context.Requestclients.Add(requestclient);

                _context.SaveChanges();

                return View("Request/FamilyFriendRequest");
            }
            else
            {
                IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);
            }
            return View("Index");

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

        public IActionResult ConciergeRequest()
        {
            return View("Request/ConciergeRequest");
        }

        public IActionResult BusinessRequest()
        {
            return View("Request/BusinessRequest");
        }
    }
}
