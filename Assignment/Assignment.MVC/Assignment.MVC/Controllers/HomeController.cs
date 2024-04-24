using Assignment.MVC.BusinessLayer.Repository.IRepository;
using Assignment.MVC.BusinessLayer.Services.Interface;
using Assignment.MVC.BusinessLayer.Utilities;
using Assignment.MVC.DataLayer.CustomModels;
using Assignment.MVC.DataLayer.DataModels;
using Assignment.MVC.DataLayer.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Assignment.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDashboardService _dashboardService;


        List<string> diseaseList = new List<string>
            { "Heart Disease", "Lung Disease" , "Kidney Disease" };

        //private readonly INotyfService _notyf;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IDashboardService dashboardService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _dashboardService = dashboardService;
        }

        public IActionResult Dashboard()
        {
            return View("Dashboard");
        }

        public async Task<IActionResult> LoadDashboardPartialTable(int pageNo, int pageSize, string? searchFilter)
        {
            PagedList<Patient> pagedList = await _dashboardService.FetchPaginatedPatientData(pageNo, pageSize, searchFilter);
            return PartialView("Partial/_DashboardPartial", pagedList);
        }

        public IActionResult LoadAddPatientModal()
        {
            PatientFormViewModel model = new PatientFormViewModel();
            model.DiseaseList = diseaseList;
            model.DoctorList = _unitOfWork.DoctorRepository.GetAll().Select(_ => _.Specialist);

            return PartialView("Partial/_PatientFormPartial", model);
        }

        public IActionResult LoadEditPatientModal(int patientId)
        {
            Patient? patient = _unitOfWork.PatientRepository.GetFirstOrDefault(pat => pat.PatientId == patientId);

            if (patient == null)
            {
                return NotFound();
            }

            PatientFormViewModel model = new PatientFormViewModel()
            {
                PatientId = patientId,
                PatientFirstName = patient.FirstName,
                PatientLastName = patient.LastName,
                Email = patient.Email,
                Age = patient.Age,
                Disease = patient.Disease,
                DoctorId = patient.DoctorId,
                Gender = patient.Gender,
                PhoneNumber = patient.PhoneNo,
                Specialist = patient.Specialist,
            };

            model.DiseaseList = diseaseList;
            model.DoctorList = _unitOfWork.DoctorRepository.GetAll().Select(_ => _.Specialist);
            model.IsEdit = true;

            return PartialView("Partial/_PatientFormPartial", model);

        }

        public bool AddPatient(PatientFormViewModel model)
        {

            try
            {

                if (ModelState.IsValid)
                {
                    _dashboardService.AddNewPatient(model);
                    TempData["success"] = NotificationMessage.PATIENT_ADDED_SUCCESSFULLY;
                    return true;
                }

                TempData["error"] = NotificationMessage.INVALID_FIELDS_ERROR;
                return false;

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return false;
            }

        }

        public bool EditPatient(PatientFormViewModel model)
        {

            try
            {
                if (ModelState.IsValid)
                {

                    ServiceResponse response = _dashboardService.EditPatientDetails(model);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        TempData["success"] = NotificationMessage.PATIENT_UPDATED_SUCCESSFULLY;
                        return true;
                    }

                    TempData["error"] = response.Message;
                    return false;

                }

                TempData["error"] = NotificationMessage.INVALID_FIELDS_ERROR;
                return false;

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return false;
            }
        }

        public bool DeletePatient(int patientId)
        {
            try
            {
                Patient? patient = _unitOfWork.PatientRepository.GetFirstOrDefault(pat => pat.PatientId == patientId);

                if (patient == null)
                {
                    TempData["error"] = NotificationMessage.PATIENT_NOT_FOUND;
                    return false;
                }

                _unitOfWork.PatientRepository.Remove(patient);
                _unitOfWork.Save();

                TempData["success"] = NotificationMessage.PATIENT_REMOVED_SUCCESSFULLY;
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return false;
            }
        }
    }
}
