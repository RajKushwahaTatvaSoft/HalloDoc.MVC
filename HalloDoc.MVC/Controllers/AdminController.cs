using Business_Layer.Interface;
using Business_Layer.Interface.Admin;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;

namespace HalloDoc.MVC.Controllers
{

    public class AdminController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IDashboardRepository _dashboardRepository;

        public AdminController(IUnitOfWork unitOfWork, IDashboardRepository dashboard)
        {
            _unitOfWork = unitOfWork;
            _dashboardRepository = dashboard;
        }

        public ActionResult PartialTable(int status)
        {
            List<AdminRequest> adminRequests = _dashboardRepository.GetAdminRequest(status);

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.adminRequests = adminRequests;
            model.DashboardStatus = status;

            return PartialView("Partial/PartialTable", model);
        }

        public IActionResult Dashboard()
        {
            AdminDashboardViewModel model = new AdminDashboardViewModel();

            model.newReqCount = _unitOfWork.RequestRepository.GetAll().Count(r => r.Status == (short)RequestStatus.Unassigned);
            model.pendingReqCount = _unitOfWork.RequestRepository.GetAll().Count(r => r.Status == (short)RequestStatus.Accepted);
            model.activeReqCount = _unitOfWork.RequestRepository.GetAll().Count(r => (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite));
            model.concludeReqCount = _unitOfWork.RequestRepository.GetAll().Count(r => r.Status == (short)RequestStatus.Conclude);
            model.toCloseReqCount = _unitOfWork.RequestRepository.GetAll().Count(r => (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed));
            model.unpaidReqCount = _unitOfWork.RequestRepository.GetAll().Count(r => r.Status == (short)RequestStatus.Unpaid);

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


        public IActionResult ViewCase(int Requestid)
        {
            if (Requestid == null)
            {
                return View("Error");
            }

            Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqFile => reqFile.Requestid == Requestid);

            ViewCaseViewModel VC = new ViewCaseViewModel();

            string dobDate = client.Intyear + "-" + client.Strmonth + "-" + client.Intdate;

            VC.patientName = client.Firstname + " " + client.Lastname;
            VC.patientFirstName = client.Firstname;
            VC.patientLastName = client.Lastname;
            VC.dob = dobDate == "--" ? null : DateTime.Parse(dobDate);
            VC.patientEmail = client.Email;
            VC.region = client.Regionid;
            VC.notes = client.Notes;
            VC.address = client.Street;
            return View("Action/ViewCase", VC);

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
