using Assignment.MVC.DataLayer.CustomModels;
using Assignment.MVC.DataLayer.DataModels;
using Assignment.MVC.DataLayer.ViewModels;

namespace Assignment.MVC.BusinessLayer.Services.Interface
{
    public interface IDashboardService
    {
        public Task<PagedList<Patient>> FetchPaginatedPatientData(int pageNo, int pageSize, string? searchFilter);
        public void AddNewPatient(PatientFormViewModel model);
        public ServiceResponse EditPatientDetails(PatientFormViewModel model);
    }
}
