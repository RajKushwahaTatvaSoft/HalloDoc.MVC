using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace HalloDoc.MVC.Controllers
{


    public class AdminController : Controller
    {

        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public ActionResult PartialTable(int status)
        {
            List<AdminRequest> adminRequests = new List<AdminRequest>();

            if (status == (int)DashboardStatus.New)
            {

                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Unassigned)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Pending)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Accepted)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Active)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Conclude)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Conclude)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.ToClose)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }
            else if (status == (int)DashboardStatus.Unpaid)
            {
                adminRequests = (from r in _context.Requests
                                 join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                                 where (r.Status == (short)RequestStatus.Unpaid)
                                 select new AdminRequest
                                 {
                                     PatientName = rc.Firstname + " " + rc.Lastname,
                                     DateOfBirth = GetPatientDOB(rc),
                                     RequestType = r.Requesttypeid,
                                     Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                     RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                     PatientPhone = rc.Phonenumber,
                                     Phone = r.Phonenumber,
                                     Address = rc.Address,
                                     Notes = rc.Notes,
                                 }).ToList();
            }

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.adminRequests = adminRequests;
            return PartialView("PartialTable", model);
        }

        public IActionResult Dashboard()
        {
            AdminDashboardViewModel model = new AdminDashboardViewModel();

            var data = (from r in _context.Requests
                        join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                        where r.Status == (short)RequestStatus.Unassigned
                        select new AdminRequest
                        {
                            PatientName = rc.Firstname + " " + rc.Lastname,
                            DateOfBirth = GetPatientDOB(rc),
                            RequestType = r.Requesttypeid,
                            Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                            RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                            PatientPhone = rc.Phonenumber,
                            Phone = r.Phonenumber,
                            Address = rc.Address,
                            Notes = rc.Notes,
                        }).ToList();

            model.adminRequests = data;
            model.newReqCount = _context.Requests.Count(r => r.Status == (short)RequestStatus.Unassigned);
            model.pendingReqCount = _context.Requests.Count(r => r.Status == (short)RequestStatus.Accepted);
            model.activeReqCount = _context.Requests.Count(r => (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite));
            model.concludeReqCount = _context.Requests.Count(r => r.Status == (short)RequestStatus.Conclude);
            model.toCloseReqCount = _context.Requests.Count(r => (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed));
            model.unpaidReqCount = _context.Requests.Count(r => r.Status == (short)RequestStatus.Unpaid);

            return View("Dashboard/Dashboard", model);

        }

        public static string GetPatientDOB(Requestclient u)
        {
            string udb = u.Intyear + "-" + u.Strmonth + "-" + u.Intdate;
            if (u.Intyear == null || u.Strmonth == null || u.Intdate == null)
            {
                return "";
            }

            DateTime dobDate = DateTime.Parse(udb);
            string dob = dobDate.ToString("MMM dd, yyyy");
            var today = DateTime.Today;
            var age = today.Year - dobDate.Year;
            if (dobDate.Date > today.AddYears(-age)) age--;

            string dobString = dob + " (" + age + ")";

            return dobString;
        }

        public static string GetRequestType(Request request)
        {
            switch (request.Requesttypeid)
            {
                case (int)RequestType.Business: return "Business";
                case (int)RequestType.Patient: return "Patient";
                case (int)RequestType.Concierge: return "Concierge";
                case (int)RequestType.Family: return "Relative/Family";
            }

            return null;
        }

        public IActionResult NewRequestStatusView()
        {
            return View("StatusPartial/NewRequestStatusView");
        }
        public IActionResult Profile()
        {
            return View("Dashboard/Profile");
        }
        public IActionResult Providers()
        {
            return View("Dashboard/Providers");
        }
        public IActionResult Partners()
        {
            return View("Dashboard/Partners");
        }

        public IActionResult ProviderLocation()
        {
            return View("Dashboard/ProviderLocation");
        }

        public IActionResult Records()
        {
            return View("Dashboard/Records");
        }

        public IActionResult Access()
        {
            return View("Dashboard/Access");
        }

        public IActionResult ViewCase()
        {
            return View("Dashboard/ViewCase");
        }
    }
}
