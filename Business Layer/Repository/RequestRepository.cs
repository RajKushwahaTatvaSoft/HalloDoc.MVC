using Business_Layer.Helpers;
using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System;
using System.Security.Cryptography;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Business_Layer.Repository
{
    public enum RequestStatus
    {
        Unassigned = 1,
        Accepted = 2,
        Cancelled = 3,
        MDEnRoute = 4,
        MDOnSite = 5,
        Conclude = 6,
        CancelledByPatient = 7,
        Closed = 8,
        Unpaid = 9,
        Clear = 10,
    }

    public enum DashboardStatus
    {
        New = 1,
        Pending = 2,
        Active = 3,
        Conclude = 4,
        ToClose = 5,
        Unpaid = 6,
    }

    public enum RequestType
    {
        Business = 1,
        Patient = 2,
        Family = 3,
        Concierge = 4
    }

    public class RequestRepository : IRequestRepository
    {
        private readonly ApplicationDbContext _context;
        public RequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public static void InsertRequestWiseFile(IFormFile document, string webRootPath)
        {
            string path = webRootPath + "/document";
            string fileName = document.FileName;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            document.CopyTo(stream);
        }
        public static string GenerateConfirmationNumber(User user)
        {
            string confirmationNumber = "AD" + user.Createddate.Date.ToString("D2") + user.Createddate.Month.ToString("D2") + user.Lastname.Substring(0, 2).ToUpper() + user.Firstname.Substring(0, 2).ToUpper() + "0001";
            return confirmationNumber;
        }
        public static string GetRequestIP()
        {
            string ip = "127.0.0.1";
            return ip;
        }
         public bool IsUserWithGivenEmailExists(string email)
        {
            bool isUserExists = _context.Aspnetusers.Any(u => u.Email == email);

            return isUserExists;

        }
        public User GetUserWithEmail(string email)
        {
            User user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user;

        }

        public User GetUserWithID(int userid)
        {
            User user = _context.Users.FirstOrDefault(u => u.Userid == userid);
            return user;

        }

        public void AddRequestForMe(MeRequestViewModel mrvm, string webRootPath, int userid)
        {
            User user = GetUserWithID(userid);
            string requestIpAddress = GetRequestIP();
            string phoneNumber = "+" + mrvm.Countrycode + '-' + mrvm.Phone;

            Request request = new()
            {
                Requesttypeid = 2,
                Userid = user.Userid,
                Confirmationnumber = GenerateConfirmationNumber(user),
                Firstname = mrvm.FirstName,
                Lastname = mrvm.LastName,
                Phonenumber = phoneNumber,
                Email = mrvm.Email,
                Status = (short)RequestStatus.Unassigned,
                Createddate = DateTime.Now,
                Patientaccountid = user.Aspnetuserid,
                Createduserid = user.Userid,
                Ip = requestIpAddress,
            };

            _context.Requests.Add(request);
            _context.SaveChanges();

            //Adding request in RequestClient Table
            Requestclient requestclient = new()
            {
                Requestid = request.Requestid,
                Firstname = mrvm.FirstName,
                Lastname = mrvm.LastName,
                Phonenumber = phoneNumber,
                Email = mrvm.Email,
                Address = mrvm.Street,
                City = mrvm.City,
                State = mrvm.State,
                Zipcode = mrvm.ZipCode,
                Notes = mrvm.Symptom,
                Ip = requestIpAddress,
            };

            _context.Requestclients.Add(requestclient);
            _context.SaveChanges();

            //Adding File Data in RequestWiseFile Table
            if (mrvm.File != null)
            {
                InsertRequestWiseFile(mrvm.File, webRootPath);

                Requestwisefile requestwisefile = new()
                {
                    Requestid = request.Requestid,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                    Filename = mrvm.File.FileName,
                };

                _context.Requestwisefiles.Add(requestwisefile);
                _context.SaveChanges();
            }


        }

        public void AddRequestForSomeoneElse(SomeoneElseRequestViewModel srvm, string webRootPath, int userid, bool isNewUser)
        {

            User relationUser = GetUserWithID(userid);
            string requestIpAddress = GetRequestIP();
            string phoneNumber = "+" + srvm.patientDetails.Countrycode + '-' + srvm.patientDetails.Phone;

            User user = null;
            if (isNewUser)
            {
                Guid generatedId = Guid.NewGuid();

                // Creating Patient in Aspnetusers Table
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = srvm.patientDetails.Email!,
                    Email = srvm.patientDetails.Email,
                    Phonenumber = phoneNumber,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Aspnetusers.Add(aspnetuser);
                _context.SaveChanges();

                // Creating Patient in User Table
                user = new()
                {
                    Aspnetuserid = generatedId.ToString(),
                    Firstname = srvm.patientDetails.FirstName,
                    Lastname = srvm.patientDetails.LastName,
                    Email = srvm.patientDetails.Email,
                    Mobile = phoneNumber,
                    Street = srvm.patientDetails.Street,
                    City = srvm.patientDetails.City,
                    State = srvm.patientDetails.State,
                    Zipcode = srvm.patientDetails.ZipCode,
                    Createddate = DateTime.Now,
                    Createdby = generatedId.ToString(),
                    Ip = requestIpAddress,
                    Intdate = srvm.patientDetails.DOB.Value.Day,
                    Strmonth = srvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = srvm.patientDetails.DOB.Value.Year,
                };

                _context.Users.Add(user);
                _context.SaveChanges();


                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Userid = relationUser.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
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

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = srvm.patientDetails.FirstName,
                    Lastname = srvm.patientDetails.LastName,
                    Phonenumber = phoneNumber,
                    Email = srvm.patientDetails.Email,
                    Address = srvm.patientDetails.Street,
                    City = srvm.patientDetails.City,
                    State = srvm.patientDetails.State,
                    Zipcode = srvm.patientDetails.ZipCode,
                    Notes = srvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();

                //Adding File Data in RequestWiseFile Table
                if (srvm.patientDetails.File != null)
                {
                    InsertRequestWiseFile(srvm.patientDetails.File, webRootPath);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = srvm.patientDetails.File.FileName,
                    };

                    _context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();
                }
            }
            else
            {
                user = GetUserWithEmail(srvm.patientDetails.Email);

                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Userid = relationUser.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
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

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = srvm.patientDetails.FirstName,
                    Lastname = srvm.patientDetails.LastName,
                    Phonenumber = phoneNumber,
                    Email = srvm.patientDetails.Email,
                    Address = srvm.patientDetails.Street,
                    City = srvm.patientDetails.City,
                    State = srvm.patientDetails.State,
                    Zipcode = srvm.patientDetails.ZipCode,
                    Notes = srvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();

                //Adding File Data in RequestWiseFile Table
                if (srvm.patientDetails.File != null)
                {
                    InsertRequestWiseFile(srvm.patientDetails.File, webRootPath);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = srvm.patientDetails.File.FileName,
                    };

                    _context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();
                }

            }

        }
        public void AddBusinessRequest(BusinessRequestViewModel brvm, string webRootPath, bool isNewUser)
        {
            User user = null;
            string requestIpAddress = GetRequestIP();
            string businessNumber = "+" + brvm.Countrycode + '-' + brvm.Phone;
            string patientNumber = "+" + brvm.patientDetails.Countrycode + '-' + brvm.patientDetails.Phone;

            if (isNewUser)
            {

                Guid generatedId = Guid.NewGuid();

                // Creating Patient in Aspnetusers Table
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = brvm.patientDetails.Email,
                    Email = brvm.patientDetails.Email,
                    Phonenumber = patientNumber,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Aspnetusers.Add(aspnetuser);
                _context.SaveChanges();


                // Creating Patient in User Table
                user = new()
                {
                    Aspnetuserid = generatedId.ToString(),
                    Firstname = brvm.patientDetails.FirstName,
                    Lastname = brvm.patientDetails.LastName,
                    Email = brvm.patientDetails.Email,
                    Mobile = patientNumber,
                    Street = brvm.patientDetails.Street,
                    City = brvm.patientDetails.City,
                    State = brvm.patientDetails.State,
                    Zipcode = brvm.patientDetails.ZipCode,
                    Createddate = DateTime.Now,
                    Createdby = generatedId.ToString(),
                    Ip = requestIpAddress,
                    Intdate = brvm.patientDetails.DOB.Value.Day,
                    Strmonth = brvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = brvm.patientDetails.DOB.Value.Year,
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Business,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = brvm.FirstName,
                    Lastname = brvm.LastName,
                    Phonenumber = businessNumber,
                    Email = brvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Casenumber = brvm.CaseNumber,
                    Patientaccountid = generatedId.ToString(),
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = brvm.patientDetails.FirstName,
                    Lastname = brvm.patientDetails.LastName,
                    Phonenumber = patientNumber,
                    Email = brvm.patientDetails.Email,
                    Address = brvm.patientDetails.Street + " " + brvm.patientDetails.City + " " + brvm.patientDetails.State + ", " + brvm.patientDetails.ZipCode,
                    Street = brvm.patientDetails.Street,
                    City = brvm.patientDetails.City,
                    State = brvm.patientDetails.State,
                    Zipcode = brvm.patientDetails.ZipCode,
                    Notes = brvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                    Intdate = brvm.patientDetails.DOB.Value.Day,
                    Strmonth = brvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = brvm.patientDetails.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();


                Business business = new()
                {
                    Name = brvm.BusinessOrPropertyName,
                    Phonenumber = brvm.Phone,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Businesses.Add(business);
                _context.SaveChanges();

                Requestbusiness reqBusiness = new()
                {
                    Requestid = request.Requestid,
                    Businessid = business.Id,
                };

                _context.Requestbusinesses.Add(reqBusiness);
                _context.SaveChanges();

            }
            else
            {

                // Fetching Registered User
                user = GetUserWithEmail(brvm.patientDetails.Email);

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Business,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = brvm.FirstName,
                    Lastname = brvm.LastName,
                    Phonenumber = businessNumber,
                    Email = brvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Casenumber = brvm.CaseNumber,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = brvm.patientDetails.FirstName,
                    Lastname = brvm.patientDetails.LastName,
                    Phonenumber = patientNumber,
                    Email = brvm.patientDetails.Email,
                    Address = brvm.patientDetails.Street + " " + brvm.patientDetails.City + " " + brvm.patientDetails.State + ", " + brvm.patientDetails.ZipCode,
                    Street = brvm.patientDetails.Street,
                    City = brvm.patientDetails.City,
                    State = brvm.patientDetails.State,
                    Zipcode = brvm.patientDetails.ZipCode,
                    Notes = brvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                    Intdate = brvm.patientDetails.DOB.Value.Day,
                    Strmonth = brvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = brvm.patientDetails.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();


                Business business = new()
                {
                    Name = brvm.BusinessOrPropertyName,
                    Phonenumber = brvm.Phone,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Businesses.Add(business);
                _context.SaveChanges();

                Requestbusiness reqBusiness = new()
                {
                    Requestid = request.Requestid,
                    Businessid = business.Id,
                };

                _context.Requestbusinesses.Add(reqBusiness);
                _context.SaveChanges();

            }

        }
        public void AddConciergeRequest(ConciergeRequestViewModel crvm, string webRootPath, bool isNewUser)
        {
            User user = null;
            string requestIpAddress = GetRequestIP();
            string conciergeNumber = "+" + crvm.Countrycode + '-' + crvm.Phone;
            string patientNumber = "+" + crvm.patientDetails.Countrycode + '-' + crvm.patientDetails.Phone;

            if (isNewUser)
            {
                Guid generatedId = Guid.NewGuid();

                // Creating Patient in Aspnetusers Table
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = crvm.patientDetails.Email,
                    Email = crvm.patientDetails.Email,
                    Phonenumber = patientNumber,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Aspnetusers.Add(aspnetuser);
                _context.SaveChanges();

                // Creating Patient in User Table
                user = new()
                {
                    Aspnetuserid = generatedId.ToString(),
                    Firstname = crvm.patientDetails.FirstName,
                    Lastname = crvm.patientDetails.LastName,
                    Email = crvm.patientDetails.Email,
                    Mobile = patientNumber,
                    Street = crvm.patientDetails.Street,
                    City = crvm.patientDetails.City,
                    State = crvm.patientDetails.State,
                    Zipcode = crvm.patientDetails.ZipCode,
                    Createddate = DateTime.Now,
                    Createdby = generatedId.ToString(),
                    Ip = requestIpAddress,
                    Intdate = crvm.patientDetails.DOB.Value.Day,
                    Strmonth = crvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = crvm.patientDetails.DOB.Value.Year,
                };

                _context.Users.Add(user);
                _context.SaveChanges();


                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Concierge,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = crvm.FirstName,
                    Lastname = crvm.LastName,
                    Phonenumber = conciergeNumber,
                    Email = crvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = generatedId.ToString(),
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = crvm.patientDetails.FirstName,
                    Lastname = crvm.patientDetails.LastName,
                    Phonenumber = patientNumber,
                    Email = crvm.patientDetails.Email,
                    Address = crvm.patientDetails.Street + " " + crvm.patientDetails.City + " " + crvm.patientDetails.State + ", " + crvm.patientDetails.ZipCode,
                    Street = crvm.patientDetails.Street,
                    City = crvm.patientDetails.City,
                    State = crvm.patientDetails.State,
                    Zipcode = crvm.patientDetails.ZipCode,
                    Notes = crvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                    Intdate = crvm.patientDetails.DOB.Value.Day,
                    Strmonth = crvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = crvm.patientDetails.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();


                Concierge concierge = new()
                {
                    Conciergename = crvm.FirstName,
                    Address = crvm.HotelOrPropertyName,
                    Street = crvm.Street,
                    City = crvm.City,
                    State = crvm.State,
                    Zipcode = crvm.ZipCode,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Concierges.Add(concierge);
                _context.SaveChanges();

                Requestconcierge reqConcierge = new()
                {
                    Requestid = request.Requestid,
                    Conciergeid = concierge.Conciergeid,
                    Ip = requestIpAddress,
                };

                _context.Requestconcierges.Add(reqConcierge);
                _context.SaveChanges();

            }
            else
            {

                // Fetching Registered User
                user = GetUserWithEmail(crvm.patientDetails.Email);

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Concierge,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = crvm.FirstName,
                    Lastname = crvm.LastName,
                    Phonenumber = conciergeNumber,
                    Email = crvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = crvm.patientDetails.FirstName,
                    Lastname = crvm.patientDetails.LastName,
                    Phonenumber = patientNumber,
                    Email = crvm.patientDetails.Email,
                    Address = crvm.patientDetails.Street + " " + crvm.patientDetails.City + " " + crvm.patientDetails.State + ", " + crvm.patientDetails.ZipCode,
                    Street = crvm.patientDetails.Street,
                    City = crvm.patientDetails.City,
                    State = crvm.patientDetails.State,
                    Zipcode = crvm.patientDetails.ZipCode,
                    Notes = crvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                    Intdate = crvm.patientDetails.DOB.Value.Day,
                    Strmonth = crvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = crvm.patientDetails.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();


                Concierge concierge = new()
                {
                    Conciergename = crvm.FirstName,
                    Address = crvm.HotelOrPropertyName,
                    Street = crvm.Street,
                    City = crvm.City,
                    State = crvm.State,
                    Zipcode = crvm.ZipCode,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Concierges.Add(concierge);
                _context.SaveChanges();

                Requestconcierge reqConcierge = new()
                {
                    Requestid = request.Requestid,
                    Conciergeid = concierge.Conciergeid,
                    Ip = requestIpAddress,
                };

                _context.Requestconcierges.Add(reqConcierge);
                _context.SaveChanges();

            }

        }
        public void AddFamilyFriendRequest(FamilyFriendRequestViewModel frvm, string webRootPath, bool isNewUser)
        {
            User user = null;
            string requestIpAddress = GetRequestIP();
            string familyNumber = "+" + frvm.Countrycode + '-' + frvm.Phone;
            string patientNumber = "+" + frvm.patientDetails.Countrycode + '-' + frvm.patientDetails.Phone;

            if (isNewUser)
            {
                Guid generatedId = Guid.NewGuid();

                // Creating Patient in Aspnetusers Table
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = frvm.patientDetails.Email,
                    Email = frvm.patientDetails.Email,
                    Phonenumber = patientNumber,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Aspnetusers.Add(aspnetuser);
                _context.SaveChanges();

                // Creating Patient in User Table
                user = new()
                {
                    Aspnetuserid = generatedId.ToString(),
                    Firstname = frvm.patientDetails.FirstName,
                    Lastname = frvm.patientDetails.LastName,
                    Email = frvm.patientDetails.Email,
                    Mobile = patientNumber,
                    Street = frvm.patientDetails.Street,
                    City = frvm.patientDetails.City,
                    State = frvm.patientDetails.State,
                    Zipcode = frvm.patientDetails.ZipCode,
                    Createddate = DateTime.Now,
                    Createdby = generatedId.ToString(),
                    Ip = requestIpAddress,
                    Intdate = frvm.patientDetails.DOB.Value.Day,
                    Strmonth = frvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = frvm.patientDetails.DOB.Value.Year,
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = frvm.FirstName,
                    Lastname = frvm.LastName,
                    Phonenumber = familyNumber,
                    Email = frvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = generatedId.ToString(),
                    Createduserid = user.Userid,
                    Relationname = frvm.Relation,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = frvm.patientDetails.FirstName,
                    Lastname = frvm.patientDetails.LastName,
                    Phonenumber = patientNumber,
                    Email = frvm.patientDetails.Email,
                    Address = frvm.patientDetails.Street + " " + frvm.patientDetails.City + " " + frvm.patientDetails.State + ", " + frvm.patientDetails.ZipCode,
                    Street = frvm.patientDetails.Street,
                    City = frvm.patientDetails.City,
                    State = frvm.patientDetails.State,
                    Zipcode = frvm.patientDetails.ZipCode,
                    Notes = frvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                    Intdate = frvm.patientDetails.DOB.Value.Day,
                    Strmonth = frvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = frvm.patientDetails.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();

                //Adding File Data in RequestWiseFile Table
                if (frvm.patientDetails.File != null)
                {
                    InsertRequestWiseFile(frvm.patientDetails.File, webRootPath);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = frvm.patientDetails.File.FileName,
                    };

                    _context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();
                }
            }
            else
            {

                // Fetching Registered User
                user = GetUserWithEmail(frvm.Email);

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Family,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = frvm.FirstName,
                    Lastname = frvm.LastName,
                    Phonenumber = familyNumber,
                    Email = frvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Relationname = frvm.Relation,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = frvm.patientDetails.FirstName,
                    Lastname = frvm.patientDetails.LastName,
                    Phonenumber = patientNumber,
                    Email = frvm.patientDetails.Email,
                    Address = frvm.patientDetails.Street + " " + frvm.patientDetails.City + " " + frvm.patientDetails.State + ", " + frvm.patientDetails.ZipCode,
                    Street = frvm.patientDetails.Street,
                    City = frvm.patientDetails.City,
                    State = frvm.patientDetails.State,
                    Zipcode = frvm.patientDetails.ZipCode,
                    Notes = frvm.patientDetails.Symptom,
                    Ip = requestIpAddress,
                    Intdate = frvm.patientDetails.DOB.Value.Day,
                    Strmonth = frvm.patientDetails.DOB.Value.Month.ToString(),
                    Intyear = frvm.patientDetails.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();

                if (frvm.patientDetails.File != null)
                {

                    InsertRequestWiseFile(frvm.patientDetails.File, webRootPath);

                    Requestwisefile reqWiseFile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = frvm.patientDetails.File.FileName,
                    };

                    _context.Requestwisefiles.Add(reqWiseFile);
                    _context.SaveChanges();
                }

            }

        }
        public void AddPatientRequest(PatientRequestViewModel prvm, string webRootPath, bool isNewUser)
        {
            string requestIpAddress = GetRequestIP();
            string phoneNumber = "+" + prvm.Countrycode + '-' + prvm.Phone;

            User user;
            if (isNewUser)
            {
                Guid generatedId = Guid.NewGuid();

                // Creating Patient in Aspnetusers Table
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = prvm.Email,
                    Passwordhash = AuthHelper.GenerateSHA256(prvm.Password),
                    Email = prvm.Email,
                    Phonenumber = phoneNumber,
                    Createddate = DateTime.Now,
                    Ip = requestIpAddress,
                };

                _context.Aspnetusers.Add(aspnetuser);
                _context.SaveChanges();

                // Creating Patient in User Table
                user = new()
                {
                    Aspnetuserid = generatedId.ToString(),
                    Firstname = prvm.FirstName,
                    Lastname = prvm.LastName,
                    Email = prvm.Email,
                    Mobile = phoneNumber,
                    Street = prvm.Street,
                    City = prvm.City,
                    State = prvm.State,
                    Zipcode = prvm.ZipCode,
                    Createddate = DateTime.Now,
                    Createdby = generatedId.ToString(),
                    Ip = requestIpAddress,
                    Intdate = prvm.DOB.Value.Day,
                    Strmonth = prvm.DOB.Value.Month.ToString(),
                    Intyear = prvm.DOB.Value.Year,
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Patient,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = prvm.FirstName,
                    Lastname = prvm.LastName,
                    Phonenumber = phoneNumber,
                    Email = prvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = generatedId.ToString(),
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = prvm.FirstName,
                    Lastname = prvm.LastName,
                    Phonenumber = phoneNumber,
                    Email = prvm.Email,
                    Address = prvm.Street + " " + prvm.City + " " + prvm.State + ", " + prvm.ZipCode,
                    Street = prvm.Street,
                    City = prvm.City,
                    State = prvm.State,
                    Zipcode = prvm.ZipCode,
                    Notes = prvm.Symptom,
                    Ip = requestIpAddress,
                    Intdate = prvm.DOB.Value.Day,
                    Strmonth = prvm.DOB.Value.Month.ToString(),
                    Intyear = prvm.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();

                //Adding File Data in RequestWiseFile Table
                if (prvm.File != null)
                {
                    InsertRequestWiseFile(prvm.File, webRootPath);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = prvm.File.FileName,
                    };

                    _context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();
                }


            }
            else
            {
                // Fetching Registered User
                user = GetUserWithEmail(prvm.Email);

                // Adding request in Request Table
                Request request = new()
                {
                    Requesttypeid = (int)RequestType.Patient,
                    Userid = user.Userid,
                    Confirmationnumber = GenerateConfirmationNumber(user),
                    Firstname = prvm.FirstName,
                    Lastname = prvm.LastName,
                    Phonenumber = phoneNumber,
                    Email = prvm.Email,
                    Status = (short)RequestStatus.Unassigned,
                    Createddate = DateTime.Now,
                    Patientaccountid = user.Aspnetuserid,
                    Createduserid = user.Userid,
                    Ip = requestIpAddress,
                };

                _context.Requests.Add(request);
                _context.SaveChanges();

                //Adding request in RequestClient Table
                Requestclient requestclient = new()
                {
                    Requestid = request.Requestid,
                    Firstname = prvm.FirstName,
                    Lastname = prvm.LastName,
                    Phonenumber = phoneNumber,
                    Email = prvm.Email,
                    Address = prvm.Street + " " + prvm.City + " " + prvm.State + ", " + prvm.ZipCode,
                    Street = prvm.Street,
                    City = prvm.City,
                    State = prvm.State,
                    Zipcode = prvm.ZipCode,
                    Notes = prvm.Symptom,
                    Ip = requestIpAddress,
                    Intdate = prvm.DOB.Value.Day,
                    Strmonth = prvm.DOB.Value.Month.ToString(),
                    Intyear = prvm.DOB.Value.Year,
                };

                _context.Requestclients.Add(requestclient);
                _context.SaveChanges();

                //Adding File Data in RequestWiseFile Table
                if (prvm.File != null)
                {
                    InsertRequestWiseFile(prvm.File, webRootPath);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now,
                        Ip = requestIpAddress,
                        Filename = prvm.File.FileName,
                    };

                    _context.Requestwisefiles.Add(requestwisefile);
                    _context.SaveChanges();
                }

            }

        }
        public MeRequestViewModel FetchRequestForMe(int userid)
        {

            User user = GetUserWithID(userid);

            string dobDate;


            if (user.Intyear == null || user.Strmonth == null || user.Intdate == null)
            {
                dobDate = null;
            }
            else
            {
                dobDate = user.Intyear + "-" + user.Strmonth + "-" + user.Intdate;
            }

            MeRequestViewModel model = new()
            {
                UserId = user.Userid,
                FirstName = user.Firstname,
                LastName = user.Lastname,
                DOB = dobDate == null ? null : DateTime.Parse(dobDate),
                Phone = user.Mobile,
                Email = user.Email,
                Street = user.Street,
                City = user.City,
                State = user.State,
                ZipCode = user.Zipcode,
            };

            return model;
        }
    }
}
