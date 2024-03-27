using Business_Layer.Interface;
using Business_Layer.Interface.AdminInterface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.ViewModels.Admin;
using Data_Layer.ViewModels.Physician;
using HalloDoc.MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace HalloDoc.MVC.Controllers
{
    [CustomAuthorize((int)AllowRole.Physician)]
    public class PhysicianController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDashboardRepository _dashboardRepository;

        public PhysicianController(IUnitOfWork unit, IDashboardRepository dashboardRepository)
        {
            _unitOfWork = unit;
            _dashboardRepository = dashboardRepository;
        }

        public IActionResult Profile()
        {
            return View("Dashboard/Profile");
        }

        public IActionResult Schedule()
        {
            return View("Dashboard/Schedule");
        }

        public IActionResult Invoicing()
        {
            return View("Dashboard/Invoicing");
        }

        public IActionResult Logout()
        {

            Response.Cookies.Delete("hallodoc");
            TempData["success"] = "Logout Successfull";

            return Redirect("/Guest/Login");
        }

        public IActionResult Dashboard()
        {
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            PhysicianDashboardViewModel model = new PhysicianDashboardViewModel();
            if (adminName != null)
            {
                model.UserName = adminName;
            }

            model.physicians = _unitOfWork.PhysicianRepository.GetAll();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.NewReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unassigned);
            model.PendingReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Accepted);
            model.ActiveReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite));
            model.ConcludeReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Conclude);
            model.ToCloseReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed));
            model.UnpaidReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unpaid);
            model.casetags = _unitOfWork.CaseTagRepository.GetAll();

            return View("Dashboard/Dashboard", model);

        }


        [HttpPost]
        public async Task<ActionResult> PartialTable(int status, int page, int typeFilter, string searchFilter, int regionFilter)
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
                RegionFilter = regionFilter,
                pageNumber = pageNumber,
                pageSize = 5,
                status = status,
            };

            PagedList<AdminRequest> pagedList = await _dashboardRepository.GetAdminRequestsAsync(filter);
             
            PhysicianDashboardViewModel model = new PhysicianDashboardViewModel();
            model.pagedList = pagedList;
            model.DashboardStatus = status;
            model.CurrentPage = pageNumber;
            model.filterOptions = filter;

            return PartialView("Partial/PartialTable", model);
        }

    }
}
