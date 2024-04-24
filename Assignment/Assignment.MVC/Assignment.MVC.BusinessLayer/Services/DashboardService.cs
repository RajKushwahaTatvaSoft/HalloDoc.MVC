using Assignment.MVC.BusinessLayer.Repository.IRepository;
using Assignment.MVC.BusinessLayer.Services.Interface;
using Assignment.MVC.BusinessLayer.Utilities;
using Assignment.MVC.DataLayer.CustomModels;
using Assignment.MVC.DataLayer.DataModels;
using Assignment.MVC.DataLayer.ViewModels;

namespace Assignment.MVC.BusinessLayer.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        public DashboardService(IUnitOfWork unit)
        {
            _unitOfWork = unit;
        }

        public async Task<PagedList<Patient>> FetchPaginatedPatientData(int pageNo, int pageSize, string? searchFilter)
        {
            pageSize = 5;
            if (pageNo < 1)
            {
                pageNo = 1;
            }

            IQueryable<Patient> patients = _unitOfWork.PatientRepository.Where(patient =>
            string.IsNullOrEmpty(searchFilter)
            || patient.FirstName.ToLower().Contains(searchFilter.ToLower())
            || patient.LastName.ToLower().Contains(searchFilter.ToLower()));

            PagedList<Patient> pagedList = await PagedList<Patient>.CreateAsync(patients, pageNo, pageSize);

            return pagedList;
        }

        public void AddNewPatient(PatientFormViewModel model)
        {

            string phoneNumber = "+" + model.CountryCode + "-" + model.PhoneNumber;

            Doctor? doctor = _unitOfWork.DoctorRepository.GetFirstOrDefault(doctor => doctor.Specialist.ToLower().Equals(model.Specialist.ToLower()));

            if (doctor == null)
            {
                doctor = new Doctor()
                {
                    Specialist = model.Specialist,
                };

                _unitOfWork.DoctorRepository.Add(doctor);
                _unitOfWork.Save();
            }

            Patient patient = new Patient()
            {
                FirstName = model.PatientFirstName,
                LastName = model.PatientLastName,
                Email = model.Email,
                Age = model.Age ?? -1,
                Gender = model.Gender,
                PhoneNo = phoneNumber,
                Disease = model.Disease,
                Specialist = model.Specialist,
                DoctorId = doctor.DoctorId,
            };

            _unitOfWork.PatientRepository.Add(patient);
            _unitOfWork.Save();

        }

        public ServiceResponse EditPatientDetails(PatientFormViewModel model)
        {

            Patient? oldPatient = _unitOfWork.PatientRepository.GetFirstOrDefault(pat => pat.PatientId == model.PatientId);

            if (oldPatient == null)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = NotificationMessage.PATIENT_NOT_FOUND,
                };
            }

            string phoneNumber = "+" + model.CountryCode + "-" + model.PhoneNumber;
            Doctor? doctor = _unitOfWork.DoctorRepository.GetFirstOrDefault(doctor => doctor.Specialist.ToLower().Equals(model.Specialist.ToLower()));

            if (doctor == null)
            {
                doctor = new Doctor()
                {
                    Specialist = model.Specialist,
                };

                _unitOfWork.DoctorRepository.Add(doctor);
                _unitOfWork.Save();
            }

            oldPatient.FirstName = model.PatientFirstName;
            oldPatient.LastName = model.PatientLastName;
            oldPatient.Email = model.Email;
            oldPatient.Age = model.Age ?? -1;
            oldPatient.Gender = model.Gender;
            oldPatient.PhoneNo = phoneNumber;
            oldPatient.Disease = model.Disease;
            oldPatient.Specialist = model.Specialist;
            oldPatient.DoctorId = doctor.DoctorId;

            _unitOfWork.PatientRepository.Update(oldPatient);
            _unitOfWork.Save();

            return new ServiceResponse
            {
                StatusCode = ResponseCode.Success
            };

        }
    }
}
