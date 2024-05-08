using Microsoft.AspNetCore.Mvc;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Mail;
using System.Net;
using Microsoft.CodeAnalysis;
using HalloDoc.MVC.Services;
using Business_Layer.Utilities;
using Business_Layer.Repository.IRepository;
using Business_Layer.Services.Patient.Interface;
using AspNetCoreHero.ToastNotification.Abstractions;
using Data_Layer.CustomModels;
using Data_Layer.CustomModels.TableRow.Patient;
using Business_Layer.Services.Helper.Interface;

namespace HalloDoc.MVC.Controllers
{

    [CustomAuthorize((int)AccountType.Patient)]
    public class PatientController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        private readonly IPatientDashboardService _dashboardRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotyfService _notyf;
        private readonly IUtilityService _utilityService;

        public PatientController(IWebHostEnvironment environment, IUtilityService utilityService, IConfiguration config, IPatientDashboardService patientDashboardRepository, IUnitOfWork unitwork, INotyfService notyfService)
        {
            _environment = environment;
            _config = config;
            _dashboardRepo = patientDashboardRepository;
            _unitOfWork = unitwork;
            _notyf = notyfService;
            _utilityService = utilityService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FetchDashboardTable(int page)
        {
            int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            int pageSize = 5;

            try
            {
                PagedList<PatientDashboardTRow> pagedList = await _dashboardRepo.GetPatientRequestsAsync(userId, page, pageSize);
                return PartialView("Partial/DashboardTable", pagedList);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Error");
            }

        }

        public IActionResult Dashboard()
        {
            return View("Dashboard/Dashboard");
        }

        public IActionResult RequestForMe()
        {
            try
            {

                int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

                if (userId == 0)
                {
                    return View("Error");
                }

                User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);

                DateTime? dobDate = DateHelper.GetDOBDateTime(user.Intyear, user.Strmonth, user.Intdate);

                IEnumerable<City> selectedCities = _unitOfWork.CityRepository.Where(city => city.Regionid == user.Regionid);
                int? cityId = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Name == user.City)?.Id;

                MeRequestViewModel model = new()
                {
                    UserId = user.Userid,
                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    DOB = dobDate,
                    Phone = user.Mobile,
                    Email = user.Email,
                    Street = user.Street,
                    State = user.State,
                    ZipCode = user.Zipcode,
                    RegionId = user.Regionid,
                    selectedRegionCities = selectedCities,
                    CityId = cityId,
                    regions = _unitOfWork.RegionRepository.GetAll(),
                };

                return View("Dashboard/RequestForMe", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestForMe(MeRequestViewModel meRequestViewModel)
        {
            try
            {


                int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

                if (userId == 0)
                {
                    return View("Error");
                }

                if (ModelState.IsValid)
                {

                    User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);
                    string requestIpAddress = RequestHelper.GetRequestIP();

                    string phone = "+" + meRequestViewModel.Countrycode + '-' + meRequestViewModel.Phone;

                    string phoneNumber = phone.Replace(" ", "");

                    string state = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == meRequestViewModel.RegionId).Name;
                    string city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == meRequestViewModel.CityId).Name;

                    Request request = new()
                    {
                        Requesttypeid = 2,
                        Userid = user.Userid,
                        Confirmationnumber = _utilityService.GenerateConfirmationNumber(user),
                        Firstname = meRequestViewModel.FirstName,
                        Lastname = meRequestViewModel.LastName,
                        Phonenumber = phoneNumber,
                        Email = meRequestViewModel.Email,
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
                        Firstname = meRequestViewModel.FirstName,
                        Lastname = meRequestViewModel.LastName,
                        Phonenumber = phoneNumber,
                        Email = meRequestViewModel.Email,
                        Address = meRequestViewModel.Street,
                        City = city,
                        Regionid = meRequestViewModel.RegionId,
                        State = state,
                        Street = meRequestViewModel.Street,
                        Strmonth = meRequestViewModel.DOB?.Month.ToString(),
                        Intdate = meRequestViewModel.DOB?.Day,
                        Intyear = meRequestViewModel.DOB?.Year,
                        Zipcode = meRequestViewModel.ZipCode,
                        Notes = meRequestViewModel.Symptom,
                        Ip = requestIpAddress,
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();

                    //Adding File Data in RequestWiseFile Table
                    if (meRequestViewModel.File != null)
                    {
                        FileHelper.InsertFileForRequest(meRequestViewModel.File, _environment.WebRootPath, request.Requestid);

                        Requestwisefile requestwisefile = new()
                        {
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now,
                            Ip = requestIpAddress,
                            Filename = meRequestViewModel.File.FileName,
                        };

                        _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                        _unitOfWork.Save();
                    }

                    _notyf.Success("Request Added Successfully.");
                    return RedirectToAction("Dashboard");
                }

                meRequestViewModel.regions = _unitOfWork.RegionRepository.GetAll();
                return View("Dashboard/RequestForMe", meRequestViewModel);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                meRequestViewModel.regions = _unitOfWork.RegionRepository.GetAll();
                return View("Dashboard/RequestForMe", meRequestViewModel);
            }
        }

        public IActionResult RequestForSomeoneElse()
        {
            try
            {
                int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

                if (userId == null)
                {
                    return View("Error");
                }

                User user = _unitOfWork.UserRepository.GetUserWithID((int)userId);

                SomeoneElseRequestViewModel model = new()
                {
                    Username = user.Firstname + " " + user.Lastname,
                    regions = _unitOfWork.RegionRepository.GetAll(),
                };

                return View("Dashboard/RequestForSomeoneElse", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestForSomeoneElse(SomeoneElseRequestViewModel srvm)
        {
            try
            {

                int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

                if (userId == 0)
                {
                    return View("Error");
                }

                if (ModelState.IsValid)
                {

                    ServiceResponse response = _dashboardRepo.SubmitRequestForSomeoneElse(srvm, userId);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success("Request Added Successfully");
                        return RedirectToAction("Dashboard");
                    }

                    _notyf.Error(response.Message);
                    srvm.regions = _unitOfWork.RegionRepository.GetAll();
                    return View("Dashboard/RequestForSomeoneElse", srvm);

                }

                srvm.regions = _unitOfWork.RegionRepository.GetAll();
                return View("Dashboard/RequestForSomeoneElse", srvm);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                srvm.regions = _unitOfWork.RegionRepository.GetAll();
                return View("Dashboard/RequestForSomeoneElse", srvm);
            }
        }

        public IActionResult ViewDocument(int requestId)
        {
            try
            {
                int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

                if (userId == 0 || requestId == 0)
                {
                    return View("Error");
                }

                User? user = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Userid == userId);
                Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);

                List<Requestwisefile> files = _unitOfWork.RequestWiseFileRepository.GetAll().Where(reqFile => reqFile.Requestid == requestId).ToList();

                ViewDocumentViewModel viewDocumentVM = new ViewDocumentViewModel();

                viewDocumentVM.requestwisefiles = files;
                viewDocumentVM.RequestId = requestId;
                viewDocumentVM.ConfirmationNumber = request?.Confirmationnumber;

                return View("Dashboard/ViewDocument", viewDocumentVM);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ViewDocument(ViewDocumentViewModel viewDocumentVM)
        {
            try
            {

                if (viewDocumentVM.File != null)
                {
                    FileHelper.InsertFileForRequest(viewDocumentVM.File, _environment.WebRootPath, viewDocumentVM.RequestId);

                    Requestwisefile requestwisefile = new()
                    {
                        Requestid = viewDocumentVM.RequestId,
                        Filename = viewDocumentVM.File.FileName,
                        Createddate = DateTime.Now,
                        Ip = RequestHelper.GetRequestIP(),

                    };
                    _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                    _unitOfWork.Save();

                    viewDocumentVM.File = null;

                }

                return ViewDocument(viewDocumentVM.RequestId);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult Profile()
        {
            try
            {

                int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
                User? user = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Userid == userId);

                if (user != null)
                {
                    DateTime? dobDate = DateHelper.GetDOBDateTime(user.Intyear, user.Strmonth, user.Intdate);

                    int? cityId = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Name.ToLower().Equals(user.City))?.Id;

                    PatientProfileViewModel model = new()
                    {
                        CityId = cityId,
                        RegionId = user.Regionid,
                        FirstName = user.Firstname,
                        LastName = user.Lastname,
                        DateOfBirth = dobDate,
                        Type = "Mobile",
                        Phone = user.Mobile,
                        Email = user.Email,
                        Street = user.Street,
                        City = user.City,
                        State = user.State,
                        ZipCode = user.Zipcode,
                        selectedCities = _unitOfWork.CityRepository.Where(city => city.Regionid == user.Regionid),
                        regions = _unitOfWork.RegionRepository.GetAll(),
                    };

                    return View("Dashboard/Profile", model);
                }

                return RedirectToAction("Error");
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(PatientProfileViewModel model)
        {

            try
            {

                int userId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
                User? dbUser = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Userid == userId);

                if (dbUser == null)
                {
                    _notyf.Error("User not found");
                    return RedirectToAction("Dashboard");
                }

                string phoneNumber = "+" + model.CountryCode + '-' + model.Phone;

                string? patientState = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == model.RegionId)?.Name;
                string? patientCity = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.CityId)?.Name;

                dbUser.Firstname = model.FirstName;
                dbUser.Lastname = model.LastName;
                dbUser.Intdate = model.DateOfBirth.Value.Day;
                dbUser.Strmonth = model.DateOfBirth.Value.Month.ToString();
                dbUser.Intyear = model.DateOfBirth.Value.Year;
                dbUser.Mobile = phoneNumber;
                dbUser.Street = model.Street;
                dbUser.Regionid = model.RegionId;
                dbUser.City = patientCity;
                dbUser.State = patientState;
                dbUser.Zipcode = model.ZipCode;

                _unitOfWork.UserRepository.Update(dbUser);
                _unitOfWork.Save();
                return RedirectToAction("Dashboard");

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult Logout()
        {

            Response.Cookies.Delete("hallodoc");
            _notyf.Success("Logout Successfull");

            return Redirect("/Guest/Login");
        }

        public async Task<IActionResult> DownloadAllFiles(int requestId)
        {
            try
            {
                // Fetch all document details for the given request:
                var documentDetails = _unitOfWork.RequestWiseFileRepository.GetAll().Where(m => m.Requestid == requestId).ToList();

                if (documentDetails == null || documentDetails.Count == 0)
                {
                    return NotFound("No documents found for download");
                }

                // Create a unique zip file name
                var zipFileName = $"Documents_{DateTime.Now:yyyyMMddHHmmss}.zip";
                var zipFilePath = Path.Combine(_environment.WebRootPath, "DownloadableZips", zipFileName);

                // Create the directory if it doesn't exist
                var zipDirectory = Path.GetDirectoryName(zipFilePath);
                if (!Directory.Exists(zipDirectory))
                {
                    Directory.CreateDirectory(zipDirectory);
                }

                // Create a new zip archive
                using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    // Add each document to the zip archive
                    foreach (var document in documentDetails)
                    {
                        var documentPath = Path.Combine(_environment.WebRootPath, "document", document.Filename);
                        zipArchive.CreateEntryFromFile(documentPath, document.Filename);
                    }
                }

                // Return the zip file for download
                var zipFileBytes = await System.IO.File.ReadAllBytesAsync(zipFilePath);
                return File(zipFileBytes, "application/zip", zipFileName);
            }
            catch
            {
                return BadRequest("Error downloading files");
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}