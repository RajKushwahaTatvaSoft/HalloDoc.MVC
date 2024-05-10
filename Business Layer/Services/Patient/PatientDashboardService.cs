using Business_Layer.Repository.IRepository;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Services.Patient.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.CustomModels.TableRow.Patient;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Hosting;

namespace Business_Layer.Services.Patient
{
    public class PatientDashboardService : IPatientDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IUtilityService _utilityService;
        private readonly IWebHostEnvironment _environment;
        public PatientDashboardService(ApplicationDbContext context, IUnitOfWork unitOfWork, IEmailService emailService, IUtilityService utilityService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _utilityService = utilityService;
            _environment = webHostEnvironment;
        }

        public async Task<PagedList<PatientDashboardTRow>> GetPatientRequestsAsync(int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }
            
            var query = (from r in _context.Requests
                         where r.Createduserid == userId
                         select new PatientDashboardTRow
                         {
                             RequestId = r.Requestid,
                             RequestStatus = RequestHelper.GetRequestStatusString(r.Status),
                             CreatedDate = r.Createddate,
                             FileCount = _context.Requestwisefiles.Count(file => file.Requestid == r.Requestid),
                         }).AsQueryable();

            return await PagedList<PatientDashboardTRow>.CreateAsync(
            query, pageNumber, pageSize);

        }

        public ServiceResponse SubmitRequestForSomeoneElse(SomeoneElseRequestViewModel model, int userId)
        {

            User? relationUser = _unitOfWork.UserRepository.GetFirstOrDefault(user => user.Userid == userId);
            if (relationUser == null)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = "User not found",
                };
            }

            Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(asp => asp.Email == model.patientDetails.Email);
            if (aspUser != null && aspUser.Accounttypeid != (int)AccountType.Patient)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = NotificationMessage.PATIENT_CANNOT_CREATED_WITH_GIVEN_EMAIL,
                };
            }

            string requestIpAddress = RequestHelper.GetRequestIP();
            string phoneNumber = "+" + model.patientDetails.Countrycode + '-' + model.patientDetails.Phone;
            string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == model.patientDetails.RegionId)?.Name;
            string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.patientDetails.CityId)?.Name;

            User? user = null;

            if (aspUser == null)
            {

                Guid generatedId = Guid.NewGuid();

                // Creating Patient in Aspnetusers Table
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = model.patientDetails.Email!,
                    Email = model.patientDetails.Email,
                    Phonenumber = phoneNumber,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                    Accounttypeid = (int)AccountType.Patient,
                };

                _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                _unitOfWork.Save();

                // Creating Patient in User Table
                user = new()
                {
                    Aspnetuserid = generatedId.ToString(),
                    Firstname = model.patientDetails.FirstName,
                    Lastname = model.patientDetails.LastName,
                    Email = model.patientDetails.Email,
                    Mobile = phoneNumber,
                    Street = model.patientDetails.Street,
                    City = city,
                    Regionid = model.patientDetails.RegionId,
                    State = state,
                    Zipcode = model.patientDetails.ZipCode,
                    Createddate = DateTime.Now,
                    Createdby = generatedId.ToString(),
                    Ip = requestIpAddress,
                    Intdate = model.patientDetails.DOB?.Day,
                    Strmonth = model.patientDetails.DOB?.Month.ToString(),
                    Intyear = model.patientDetails.DOB?.Year,
                };

                _unitOfWork.UserRepository.Add(user);
                _unitOfWork.Save();

                _emailService.SendMailForCreateAccount(model.patientDetails.Email, aspnetuser.Id, "");

                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Userid = relationUser.Userid,
                    Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                    Firstname = relationUser.Firstname,
                    Lastname = relationUser.Lastname,
                    Phonenumber = relationUser.Mobile,
                    Email = relationUser.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _unitOfWork.RequestRepository.Add(request);
                _unitOfWork.Save();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = model.patientDetails.FirstName,
                    Lastname = model.patientDetails.LastName,
                    Phonenumber = phoneNumber,
                    Email = model.patientDetails.Email,
                    Address = model.patientDetails.Street,
                    City = city,
                    Regionid = model.patientDetails.RegionId,
                    State = state,
                    Strmonth = model.patientDetails.DOB?.Month.ToString(),
                    Intdate = model.patientDetails.DOB?.Day,
                    Intyear = model.patientDetails.DOB?.Year,
                    Zipcode = model.patientDetails.ZipCode,
                    Notes = model.patientDetails.Symptom,
                    Ip = requestIpAddress,
                };

                _unitOfWork.RequestClientRepository.Add(requestclient);
                _unitOfWork.Save();

                //Adding File Data in RequestWiseFile Table
                if (model.patientDetails.File != null)
                {
                    FileHelper.InsertFileForRequest(model.patientDetails.File, _environment.WebRootPath, request.Requestid);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = model.patientDetails.File.FileName,
                    };

                    _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                    _unitOfWork.Save();
                }

            }
            else
            {

                user = _unitOfWork.UserRepository.GetUserWithEmail(model.patientDetails.Email);

                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Userid = relationUser.Userid,
                    Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                    Firstname = relationUser.Firstname,
                    Lastname = relationUser.Lastname,
                    Phonenumber = relationUser.Mobile,
                    Email = relationUser.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _unitOfWork.RequestRepository.Add(request);
                _unitOfWork.Save();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = model.patientDetails.FirstName,
                    Lastname = model.patientDetails.LastName,
                    Phonenumber = phoneNumber,
                    Email = model.patientDetails.Email,
                    Address = model.patientDetails.Street,
                    City = city,
                    Regionid = model.patientDetails.RegionId,
                    State = state,
                    Zipcode = model.patientDetails.ZipCode,
                    Notes = model.patientDetails.Symptom,
                    Ip = requestIpAddress,
                };

                _unitOfWork.RequestClientRepository.Add(requestclient);
                _unitOfWork.Save();

                //Adding File Data in RequestWiseFile Table
                if (model.patientDetails.File != null)
                {
                    FileHelper.InsertFileForRequest(model.patientDetails.File, _environment.WebRootPath, request.Requestid);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = model.patientDetails.File.FileName,
                    };

                    _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                    _unitOfWork.Save();
                }

            }

            return new ServiceResponse
            {
                StatusCode = ResponseCode.Success
            };
        }
    }
}
