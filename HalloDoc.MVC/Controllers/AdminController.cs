using Business_Layer.Interface;
using Business_Layer.Interface.Admin;
using Business_Layer.Repository.Admin;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Utilities;

namespace HalloDoc.MVC.Controllers
{
    public enum TypeFilter
    {
        All = 0,
        Patient = 1,
        FamilyFriend = 2,
        Concierge = 3,
        Business = 4,
        VIP = 5,
    }

    public class AdminController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IDashboardRepository _dashboardRepository;

        public AdminController(IUnitOfWork unitOfWork, IDashboardRepository dashboard, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _dashboardRepository = dashboard;
            _context = context;
        }

        [HttpPost]
        public ActionResult PartialTable(int status, int page, int typeFilter, string searchFilter)
        {
            int pageNumber = 1;
            if (page > 0)
            {
                pageNumber = page;
            }

            DashboardFilter filter = new DashboardFilter()
            {
                RequestTypeFilter = typeFilter,
                PatientSearchText = searchFilter,
                RegionFilter = 0,
            };

            List<AdminRequest> adminRequests = _dashboardRepository.GetAdminRequest(status, pageNumber, filter);

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.adminRequests = adminRequests;
            model.DashboardStatus = status;
            model.CurrentPage = pageNumber;
            model.filterOptions = filter;

            return PartialView("Partial/PartialTable", model);
        }

        [HttpPost]
        public ActionResult LoadNextPage(int status, int page, int typeFilter, string searchFilter)
        {
            page = page + 1;
            return PartialTable(status, page, typeFilter, searchFilter);
        }

        [HttpPost]
        public ActionResult LoadPreviousPage(int status, int page, int typeFilter, string searchFilter)
        {
            page = page - 1;
            return PartialTable(status, page, typeFilter, searchFilter);
        }


        public IActionResult Dashboard()
        {

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.casetags = _unitOfWork.CaseTagRepository.GetAll();
            model.physicians = _context.Physicians;
            model.regions = _context.Regions;
            model.NewReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unassigned);
            model.PendingReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Accepted);
            model.ActiveReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite));
            model.ConcludeReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Conclude);
            model.ToCloseReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed));
            model.UnpaidReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unpaid);
            model.regions = _context.Regions;
            model.physicians = _context.Physicians;
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

        [HttpPost]
        public bool CancelCaseModal(int reason, string notes, int requestid)
        {
            int adminId = 1;
            try
            {

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);
                req.Status = (short)RequestStatus.Cancelled;
                req.Modifieddate = DateTime.Now;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestid,
                    Status = (short)RequestStatus.Cancelled,
                    Adminid = adminId,
                    Notes = notes,
                    Physicianid = req.Physicianid,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Cancelled";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while cancelling request.";
                return false;
            }

            return false;

        }

        [HttpPost]
        public bool AssignCaseModal(string notes, int requestid, int physicianid)
        {
            int adminId = 1;
            if(requestid == null || requestid <= 0 || physicianid == null ||  physicianid <= 0)
            {
                TempData["error"] = "Error occured while assigning request.";
                return false;
            }
            try
            {

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);
                req.Status = (short)RequestStatus.Accepted;
                req.Modifieddate = DateTime.Now;
                req.Physicianid = physicianid;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestid,
                    Status = (short)RequestStatus.Accepted,
                    Adminid = adminId,
                    Notes = notes,
                    Physicianid = req.Physicianid,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();


                TempData["success"] = "Request Successfully Assigned.";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while assigning request.";
                return false;
            }

            return false;

        }

        [HttpPost]
        public bool BlockPatient(string reason, int requestid)
        {
            int adminId = 1;
            try
            {

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);
                req.Status = (short)RequestStatus.Block;
                req.Modifieddate = DateTime.Now;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();


                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestid,
                    Status = (short)RequestStatus.Cancelled,
                    Adminid = adminId,
                    Notes = reason,
                    Physicianid = req.Physicianid,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                Requestclient reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == requestid);

                Blockrequest blockrequest = new Blockrequest()
                {
                    Phonenumber = reqCli.Phonenumber,
                    Email = reqCli.Email,
                    Reason = reason,
                    Requestid = reqCli.Requestid.ToString(),
                    Createddate = DateTime.Now,
                };

                _context.Blockrequests.Add(blockrequest);
                _context.SaveChanges();

                TempData["success"] = "Request Successfully Blocked";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while blocking request.";
                return false;
            }

            return false;

        }

        public IActionResult ViewCase(int Requestid)
        {
            if (Requestid == null)
            {
                return View("Error");
            }

            Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqFile => reqFile.Requestid == Requestid);

            ViewCaseViewModel model = new();

            string dobDate = client.Intyear + "-" + client.Strmonth + "-" + client.Intdate;

            model.patientName = client.Firstname + " " + client.Lastname;
            model.patientFirstName = client.Firstname;
            model.patientLastName = client.Lastname;
            model.dob = dobDate == "--" ? null : DateTime.Parse(dobDate);
            model.patientEmail = client.Email;
            model.region = client.Regionid;
            model.notes = client.Notes;
            model.address = client.Street;
            return View("Action/ViewCase", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ViewCase(ViewCaseViewModel viewCase)
        {
            if (viewCase != null)
            {

                string phoneNumber = "+" + viewCase.countryCode + '-' + viewCase.patientPhone;

                Requestclient reqcli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(req => req.Requestid == viewCase.requestId);
                reqcli.Notes = viewCase.notes;

                _unitOfWork.RequestClientRepository.Update(reqcli);
                _unitOfWork.Save();

                return ViewCase(viewCase.requestId);

            }
            return View("Error");

        }

        public IActionResult ViewNotes()
        {
            return View("Action/ViewNotes");
        }

    }
}
