using Business_Layer.Repository.IRepository;
using Business_Layer.Services.Guest.Interface;
using Business_Layer.Services.Helper;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Guest;
using Microsoft.AspNetCore.Hosting;

namespace Business_Layer.Services.Guest
{
    public class RequestService : IRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UtilityService _utilityService;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        public RequestService(IUnitOfWork unitOfWork, IWebHostEnvironment environment, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _utilityService = new UtilityService(unitOfWork);
            _environment = environment;
            _emailService = emailService;
        }

        public ServiceResponse SubmitPatientRequest(PatientRequestViewModel model)
        {

            string requestIpAddress = RequestHelper.GetRequestIP();
            string phoneNumber = "+" + model.Countrycode + '-' + model.Phone;
            string? patientState = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == model.RegionId)?.Name;
            string? patientCity = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.CityId)?.Name;

            bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(model.Email);

            if (!isUserExists)
            {
                if (model.Password != null)
                {
                    User user;

                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = model.Email,
                        Passwordhash = AuthHelper.GenerateSHA256(model.Password),
                        Email = model.Email,
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
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Email = model.Email,
                        Mobile = phoneNumber,
                        Street = model.Street,
                        State = patientState,
                        Regionid = model.RegionId,
                        City = patientCity,
                        Zipcode = model.ZipCode,
                        Createddate = DateTime.Now,
                        Createdby = generatedId.ToString(),
                        Ip = requestIpAddress,
                        Intdate = model.DOB?.Day,
                        Strmonth = model.DOB?.Month.ToString(),
                        Intyear = model.DOB?.Year,
                    };

                    _unitOfWork.UserRepository.Add(user);
                    _unitOfWork.Save();

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Patient,
                        Userid = user.Userid,
                        Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = phoneNumber,
                        Email = model.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = generatedId.ToString(),
                        Createduserid = user.Userid,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestRepository.Add(request);
                    _unitOfWork.Save();

                    //Adding request in RequestClient Table
                    Requestclient requestclient = new()
                    {
                        Requestid = request.Requestid,
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = phoneNumber,
                        Email = model.Email,
                        Address = model.Street + " " + patientCity + " " + patientState + ", " + model.ZipCode,
                        Street = model.Street,
                        Regionid = model.RegionId,
                        City = patientCity,
                        State = patientState,
                        Zipcode = model.ZipCode,
                        Notes = model.Symptom,
                        Ip = requestIpAddress,
                        Intdate = model.DOB?.Day,
                        Strmonth = model.DOB?.Month.ToString(),
                        Intyear = model.DOB?.Year,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    //Adding File Data in RequestWiseFile Table
                    if (model.File != null)
                    {
                        FileHelper.InsertFileForRequest(model.File, _environment.WebRootPath, request.Requestid);

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = model.File.FileName,
                        };

                        _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                        _unitOfWork.Save();
                    }

                    return new ServiceResponse
                    {
                        StatusCode = ResponseCode.Success,
                        Message = "Request Created Successfully",
                    };
                }
                else
                {
                    return new ServiceResponse
                    {
                        StatusCode = ResponseCode.Error,
                        Message = "Password Can't be Empty.",
                    };
                }
            }
            else
            {
                User user;

                // Fetching Registered User
                user = _unitOfWork.UserRepository.GetUserWithEmail(model.Email);

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Patient,
                    Userid = user.Userid,
                    Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                    Firstname = model.FirstName,
                    Lastname = model.LastName,
                    Phonenumber = phoneNumber,
                    Email = model.Email,
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
                    Firstname = model.FirstName,
                    Lastname = model.LastName,
                    Phonenumber = phoneNumber,
                    Email = model.Email,
                    Address = model.Street + " " + patientCity + " " + patientState + ", " + model.ZipCode,
                    Street = model.Street,
                    City = patientCity,
                    Regionid = model.RegionId,
                    State = patientState,
                    Zipcode = model.ZipCode,
                    Notes = model.Symptom,
                    Ip = requestIpAddress,
                    Intdate = model.DOB?.Day,
                    Strmonth = model.DOB?.Month.ToString(),
                    Intyear = model.DOB?.Year,
                };

                _unitOfWork.RequestClientRepository.Add(requestclient);
                _unitOfWork.Save();

                //Adding File Data in RequestWiseFile Table
                if (model.File != null)
                {
                    FileHelper.InsertFileForRequest(model.File, _environment.WebRootPath, request.Requestid);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = model.File.FileName,
                    };

                    _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                    _unitOfWork.Save();
                }
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Success,
                    Message = "Request Created Successfully.",
                };

            }

        }

        public ServiceResponse SubmitFamilyFriendRequest(FamilyFriendRequestViewModel model, string createAccLink)
        {
            try
            {

                User user;
                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(model.patientDetails.Email);
                string requestIpAddress = RequestHelper.GetRequestIP();
                string familyNumber = "+" + model.Countrycode + '-' + model.Phone;
                string patientNumber = "+" + model.patientDetails.Countrycode + '-' + model.patientDetails.Phone;
                string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == model.patientDetails.RegionId)?.Name;
                string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.patientDetails.CityId)?.Name;

                if (!isUserExists)
                {
                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = model.patientDetails.Email,
                        Email = model.patientDetails.Email,
                        Phonenumber = patientNumber,
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
                        Mobile = patientNumber,
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

                    _emailService.SendMailForCreateAccount(model.patientDetails.Email, aspnetuser.Id, createAccLink);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Family,
                        Userid = user.Userid,
                        Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = familyNumber,
                        Email = model.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = generatedId.ToString(),
                        Createduserid = user.Userid,
                        Relationname = model.Relation,
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
                        Phonenumber = patientNumber,
                        Email = model.patientDetails.Email,
                        Address = model.patientDetails.Street + " " + city + " " + state + ", " + model.patientDetails.ZipCode,
                        Street = model.patientDetails.Street,
                        City = city,
                        Regionid = model.patientDetails.RegionId,
                        State = state,
                        Zipcode = model.patientDetails.ZipCode,
                        Notes = model.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = model.patientDetails.DOB?.Day,
                        Strmonth = model.patientDetails.DOB?.Month.ToString(),
                        Intyear = model.patientDetails.DOB?.Year,
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
                    string message = "Email Successfully sent to " + user.Email + " for creating account.";
                    return new ServiceResponse()
                    {
                        StatusCode = ResponseCode.Success,
                        Message = message,
                    };

                }
                // Fetching Registered User
                user = _unitOfWork.UserRepository.GetUserWithEmail(model.patientDetails.Email);

                // Adding request in Request Table
                Request requestExistedUser = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Userid = user.Userid,
                    Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                    Firstname = model.FirstName,
                    Lastname = model.LastName,
                    Phonenumber = familyNumber,
                    Email = model.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Relationname = model.Relation,
                    Ip = requestIpAddress,
                };


                _unitOfWork.RequestRepository.Add(requestExistedUser);
                _unitOfWork.Save();

                Requestclient requestclientExistedUser = new()
                {
                    Requestid = requestExistedUser.Requestid,
                    Firstname = model.patientDetails.FirstName,
                    Lastname = model.patientDetails.LastName,
                    Phonenumber = patientNumber,
                    Email = model.patientDetails.Email,
                    Address = model.patientDetails.Street + " " + city + " " + state + ", " + model.patientDetails.ZipCode,
                    Street = model.patientDetails.Street,
                    City = city,
                    Regionid = model.patientDetails.RegionId,
                    State = state,
                    Zipcode = model.patientDetails.ZipCode,
                    Notes = model.patientDetails.Symptom,
                    Ip = requestIpAddress,
                    Intdate = model.patientDetails.DOB?.Day,
                    Strmonth = model.patientDetails.DOB?.Month.ToString(),
                    Intyear = model.patientDetails.DOB?.Year,
                };


                _unitOfWork.RequestClientRepository.Add(requestclientExistedUser);
                _unitOfWork.Save();

                if (model.patientDetails.File != null)
                {

                    FileHelper.InsertFileForRequest(model.patientDetails.File, _environment.WebRootPath, requestExistedUser.Requestid);

                    Requestwisefile reqWiseFile = new()
                    {
                        Requestid = requestExistedUser.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = model.patientDetails.File.FileName,
                    };


                    _unitOfWork.RequestWiseFileRepository.Add(reqWiseFile);
                    _unitOfWork.Save();
                }

                return new ServiceResponse()
                {
                    StatusCode = ResponseCode.Success,
                    Message = "Request Created Successfully",
                };
            }
            catch (Exception e)
            {

                return new ServiceResponse()
                {
                    StatusCode = ResponseCode.Error,
                    Message = e.Message,
                };
            }

        }

        public ServiceResponse SubmitConciergeRequest(ConciergeRequestViewModel model, string createAccLink)
        {
            try
            {

                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(model.patientDetails.Email);

                User user;
                string requestIpAddress = RequestHelper.GetRequestIP();
                string conciergeNumber = "+" + model.Countrycode + '-' + model.Phone;
                string patientNumber = "+" + model.patientDetails.Countrycode + '-' + model.patientDetails.Phone;
                string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == model.patientDetails.RegionId)?.Name;
                string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.patientDetails.CityId)?.Name;

                if (!isUserExists)
                {
                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = model.patientDetails.Email,
                        Email = model.patientDetails.Email,
                        Phonenumber = patientNumber,
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
                        Mobile = patientNumber,
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

                    _emailService.SendMailForCreateAccount(model.patientDetails.Email, aspnetuser.Id, createAccLink);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Concierge,
                        Userid = user.Userid,
                        Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = conciergeNumber,
                        Email = model.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Patientaccountid = generatedId.ToString(),
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
                        Phonenumber = patientNumber,
                        Email = model.patientDetails.Email,
                        Address = model.patientDetails.Street + " " + city + " " + state + ", " + model.patientDetails.ZipCode,
                        Street = model.patientDetails.Street,
                        City = city,
                        Regionid = model.patientDetails.RegionId,
                        State = state,
                        Zipcode = model.patientDetails.ZipCode,
                        Notes = model.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = model.patientDetails.DOB?.Day,
                        Strmonth = model.patientDetails.DOB?.Month.ToString(),
                        Intyear = model.patientDetails.DOB?.Year,
                    };


                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    Concierge concierge = new()
                    {
                        Conciergename = model.FirstName,
                        Address = model.HotelOrPropertyName,
                        Street = model.patientDetails.Street,
                        City = city,
                        Regionid = model.patientDetails.RegionId,
                        State = state,
                        Zipcode = model.patientDetails.ZipCode,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.ConciergeRepository.Add(concierge);
                    _unitOfWork.Save();

                    Requestconcierge reqConcierge = new()
                    {
                        Requestid = request.Requestid,
                        Conciergeid = concierge.Conciergeid,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestConciergeRepository.Add(reqConcierge);
                    _unitOfWork.Save();

                    string message = "Email Successfully sent to " + user.Email + " for creating account.";
                    return new ServiceResponse()
                    {
                        StatusCode = ResponseCode.Success,
                        Message = message,
                    };
                }
                else
                {

                    // Fetching Registered User
                    user = _unitOfWork.UserRepository.GetUserWithEmail(model.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Concierge,
                        Userid = user.Userid,
                        Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = conciergeNumber,
                        Email = model.Email,
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
                        Phonenumber = patientNumber,
                        Email = model.patientDetails.Email,
                        Address = model.patientDetails.Street + " " + city + " " + state + ", " + model.patientDetails.ZipCode,
                        Street = model.patientDetails.Street,
                        City = city,
                        Regionid = model.patientDetails.RegionId,
                        State = state,
                        Zipcode = model.patientDetails.ZipCode,
                        Notes = model.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = model.patientDetails.DOB?.Day,
                        Strmonth = model.patientDetails.DOB?.Month.ToString(),
                        Intyear = model.patientDetails.DOB?.Year,
                    };


                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    Concierge concierge = new()
                    {
                        Conciergename = model.FirstName,
                        Address = model.HotelOrPropertyName,
                        Street = model.patientDetails.Street ?? "",
                        City = city ?? "",
                        Regionid = model.patientDetails.RegionId,
                        State = state ?? "",
                        Zipcode = model.patientDetails.ZipCode ?? "",
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                    };
                    _unitOfWork.ConciergeRepository.Add(concierge);
                    _unitOfWork.Save();



                    Requestconcierge reqConcierge = new()
                    {
                        Requestid = request.Requestid,
                        Conciergeid = concierge.Conciergeid,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestConciergeRepository.Add(reqConcierge);
                    _unitOfWork.Save();


                    return new ServiceResponse()
                    {
                        StatusCode = ResponseCode.Success,
                        Message = "Request Created Successfully",
                    };
                }


            }
            catch (Exception e)
            {
                return new ServiceResponse()
                {
                    StatusCode = ResponseCode.Error,
                    Message = e.Message,
                };
            }
        }

        public ServiceResponse SubmitBusinessRequest(BusinessRequestViewModel model, string createAccLink)
        {
            try
            {

                bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(model.patientDetails.Email);

                User user;
                string requestIpAddress = RequestHelper.GetRequestIP();
                string businessNumber = "+" + model.Countrycode + '-' + model.Phone;
                string patientNumber = "+" + model.patientDetails.Countrycode + '-' + model.patientDetails.Phone;
                string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == model.patientDetails.RegionId)?.Name;
                string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.patientDetails.CityId)?.Name;


                if (!isUserExists)
                {

                    Guid generatedId = Guid.NewGuid();

                    // Creating Patient in Aspnetusers Table
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = model.patientDetails.Email,
                        Email = model.patientDetails.Email,
                        Phonenumber = patientNumber,
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
                        Mobile = patientNumber,
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

                    _emailService.SendMailForCreateAccount(model.patientDetails.Email, aspnetuser.Id, createAccLink);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Business,
                        Userid = user.Userid,
                        Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = businessNumber,
                        Email = model.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Casenumber = model.CaseNumber,
                        Patientaccountid = generatedId.ToString(),
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
                        Phonenumber = patientNumber,
                        Email = model.patientDetails.Email,
                        Address = model.patientDetails.Street + " " + city + " " + state + ", " + model.patientDetails.ZipCode,
                        Street = model.patientDetails.Street,
                        City = city,
                        Regionid = model.patientDetails.RegionId,
                        State = state,
                        Zipcode = model.patientDetails.ZipCode,
                        Notes = model.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = model.patientDetails.DOB?.Day,
                        Strmonth = model.patientDetails.DOB?.Month.ToString(),
                        Intyear = model.patientDetails.DOB?.Year,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    Business business = new()
                    {
                        Name = model.BusinessOrPropertyName,
                        Phonenumber = model.Phone,
                        Createddate = DateTime.Now,
                        City = model.BusinessOrPropertyName,
                        Regionid = model.patientDetails.RegionId,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.BusinessRepo.Add(business);
                    _unitOfWork.Save();

                    Requestbusiness reqBusiness = new()
                    {
                        Requestid = request.Requestid,
                        Businessid = business.Id,
                    };

                    _unitOfWork.RequestBusinessRepo.Add(reqBusiness);
                    _unitOfWork.Save();

                    string message = "Email Successfully sent to " + user.Email + " for creating account.";
                    return new ServiceResponse()
                    {
                        StatusCode = ResponseCode.Success,
                        Message = message,
                    };
                }
                else
                {

                    // Fetching Registered User
                    user = _unitOfWork.UserRepository.GetUserWithEmail(model.patientDetails.Email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = (int)RequestType.Business,
                        Userid = user.Userid,
                        Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = businessNumber,
                        Email = model.Email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Casenumber = model.CaseNumber,
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
                        Phonenumber = patientNumber,
                        Email = model.patientDetails.Email,
                        Address = model.patientDetails.Street + " " + city + " " + state + ", " + model.patientDetails.ZipCode,
                        Street = model.patientDetails.Street,
                        City = city,
                        Regionid = model.patientDetails.RegionId,
                        State = state,
                        Zipcode = model.patientDetails.ZipCode,
                        Notes = model.patientDetails.Symptom,
                        Ip = requestIpAddress,
                        Intdate = model.patientDetails.DOB?.Day,
                        Strmonth = model.patientDetails.DOB?.Month.ToString(),
                        Intyear = model.patientDetails.DOB?.Year,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    Business business = new()
                    {
                        Name = model.BusinessOrPropertyName,
                        Phonenumber = model.Phone,
                        Createddate = DateTime.Now,
                        City = model.BusinessOrPropertyName,
                        Regionid = model.patientDetails.RegionId,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.BusinessRepo.Add(business);
                    _unitOfWork.Save();

                    Requestbusiness reqBusiness = new()
                    {
                        Requestid = request.Requestid,
                        Businessid = business.Id,
                    };

                    _unitOfWork.RequestBusinessRepo.Add(reqBusiness);
                    _unitOfWork.Save();

                    return new ServiceResponse
                    {
                        StatusCode = ResponseCode.Success,
                        Message = NotificationMessage.REQUEST_CREATED_SUCCESSFULLY,
                    };
                }

            }
            catch (Exception e)
            {
                return new ServiceResponse()
                {
                    StatusCode = ResponseCode.Error,
                    Message = e.Message,
                };
            }
        }
        
    }
}
