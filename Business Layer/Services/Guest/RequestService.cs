using Business_Layer.Repository.IRepository;
using Business_Layer.Services.Guest.Interface;
using Business_Layer.Services.Helper;
using Business_Layer.Utilities;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Business_Layer.Services.Guest
{
    public class RequestService : IRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UtilityService _utilityService;
        private readonly IWebHostEnvironment _environment;
        public RequestService(IUnitOfWork unitOfWork, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _utilityService = new UtilityService(unitOfWork);
            _environment = environment;
        }

        public Dictionary<string, object> SubmitPatientRequest(PatientRequestViewModel model)
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

                    return new Dictionary<string, object>(){
                                  {"success", true},
                                  {"errorMessage", ""}
                    };
                }
                else
                {
                    return new Dictionary<string, object>(){
                                  {"success", false},
                                  {"errorMessage", "Password Cannot be Empty."}
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
                return new Dictionary<string, object>(){
                                  {"success", true},
                                  {"errorMessage", ""}
                    };

            }

        }
    }
}
