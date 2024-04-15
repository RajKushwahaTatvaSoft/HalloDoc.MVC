using AspNetCoreHero.ToastNotification.Abstractions;
using Business_Layer.Repository.IRepository;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.CustomModels.TableRow.Physician;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Data_Layer.ViewModels.Physician;
using HalloDoc.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Text.Json.Nodes;
using Business_Layer.Services.Helper.Interface;
using System.Text;
using Rotativa.AspNetCore;
using Business_Layer.Services.Physician.Interface;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Services.AdminServices;

namespace HalloDoc.MVC.Controllers
{
    [CustomAuthorize((int)AccountType.Physician)]
    public class PhysicianController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly INotyfService _notyf;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;
        private readonly IPhysicianService _physicianService;
        private readonly string REQUEST_FILE_PATH = Path.Combine("document", "request");

        public PhysicianController(IUnitOfWork unit, IPhysicianService physicianService, IAdminDashboardService dashboardRepository, IConfiguration config, IPhysicianDashboardService physicalDashboardService, INotyfService notyf, IWebHostEnvironment webHostEnvironment, IEmailService emailService)
        {
            _unitOfWork = unit;
            _config = config;
            _notyf = notyf;
            _environment = webHostEnvironment;
            _emailService = emailService;
            _physicianService = physicianService;
        }

        #region Profile

        public IActionResult Profile()
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

            Physician physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Aspnetuserid == phyAspId);


            if (physician == null)
            {
                return View("Error");
            }

            Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Id == physician.Aspnetuserid);

            if (aspUser == null)
            {
                return View("Error");
            }

            int? cityId = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Name == physician.City)?.Id;

            IEnumerable<int> phyRegions = _unitOfWork.PhysicianRegionRepo.Where(pr => pr.Physicianid == physician.Physicianid).ToList().Select(_ => (int)_.Regionid); ;

            EditPhysicianViewModel model = new EditPhysicianViewModel()
            {
                UserName = aspUser.Username,
                PhysicianId = physician.Physicianid,
                FirstName = physician.Firstname,
                LastName = physician.Lastname,
                Email = physician.Email,
                Phone = physician.Mobile,
                MedicalLicenseNumber = physician.Medicallicense,
                NPINumber = physician.Npinumber,
                SyncEmail = physician.Syncemailaddress,
                Address1 = physician.Address1,
                Address2 = physician.Address2,
                RegionId = physician.Regionid,
                Zip = physician.Zip,
                RoleId = (int)physician.Roleid,
                MailPhone = physician.Altphone,
                BusinessName = physician.Businessname,
                CityId = cityId,
                BusinessWebsite = physician.Businesswebsite,
                regions = _unitOfWork.RegionRepository.GetAll(),
                roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Physician),
                physicianRegions = phyRegions,
                IsICA = physician.Isagreementdoc ?? false,
                IsBGCheck = physician.Isbackgrounddoc ?? false,
                IsHIPAA = physician.Iscredentialdoc ?? false,
                IsLicenseDoc = physician.Islicensedoc ?? false,
                IsNDA = physician.Isnondisclosuredoc ?? false,
                selectedRegions = _unitOfWork.PhysicianRegionRepo.Where(pr => pr.Physicianid == physician.Physicianid).Select(_ => _.Regionid),
            };

            return View("Header/Profile", model);
        }

        public bool ResetPhysicianPassword(string? updatePassword)
        {

            try
            {
                if (string.IsNullOrEmpty(updatePassword))
                {
                    return false;
                }
                string? phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

                Aspnetuser? aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Id == phyAspId);

                if(aspUser == null)
                {
                    return false;
                }

                aspUser.Passwordhash = AuthHelper.GenerateSHA256(updatePassword);
                _unitOfWork.AspNetUserRepository.Update(aspUser);
                _unitOfWork.Save();

                _notyf.Success("Password Updated Successfully.");

                return true;
            }
            catch (Exception e)
            {


                return false;
            }

        }

        [HttpPost]
        public bool SendMessageToAdmin(string message)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyName= HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            IEnumerable<Admin> admins = _unitOfWork.AdminRepository.GetAll();

            string subject = "Need To Edit My Profile";
            string? senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
            string? senderPassword = _config.GetSection("OutlookSMTP")["Password"];

            string editPhysicianProfileLink = Url.Action("EditPhysicianAccount", "Admin", new { physicianId = phyId }, Request.Scheme);

            string body = "<p>" +
                "Provider " + phyName + " sent message regarding changing his profile: " + message +
                "</p>" +
                "<a href=\"" + editPhysicianProfileLink + "\" >Click here to edit physician profile</a>";

            SmtpClient client = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "HalloDoc"),
                Subject = subject,
                IsBodyHtml = true,
                Body = body,
            };

            foreach (Admin admin in admins)
            {
                string email = admin.Email;

                if (!string.IsNullOrEmpty(email))
                {
                    mailMessage.To.Add(email);

                    Emaillog emaillog = new()
                    {
                        Emailtemplate = "r",
                        Subjectname = subject,
                        Emailid = email,
                        Roleid = admin.Roleid,
                        Adminid = admin.Adminid,
                        Createdate = DateTime.Now,
                        Sentdate = DateTime.Now,
                        Isemailsent = true,
                    };
                    _unitOfWork.EmailLogRepository.Add(emaillog);
                }

            }

            client.Send(mailMessage);
            _unitOfWork.Save();


            return false;
        }

        #endregion

        #region Schedule

        public IActionResult ShowDayShiftsModal(DateTime jsDate)
        {
            DateTime shiftDate = jsDate.ToLocalTime().Date;
            DayShiftModel model = new DayShiftModel()
            {
                ShiftDate = shiftDate,
                shiftdetails = _unitOfWork.ShiftDetailRepository.Where(shift => shift.Shiftdate == shiftDate),
            };

            return PartialView("Schedule/DayShiftModal", model);
        }

        public IActionResult Schedule()
        {

            SchedulingViewModel model = new SchedulingViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();

            return View("Schedule/MySchedule", model);
        }

        public IActionResult LoadMonthSchedule(int shiftMonth, int shiftYear)
        {
            // 0 index'ed month to 1 index'ed month
            shiftMonth++;
            IEnumerable<Shiftdetail> query = _unitOfWork.ShiftDetailRepository.Where(shift => shift.Shiftdate.Month == shiftMonth && shift.Shiftdate.Year == shiftYear);

            int days = DateTime.DaysInMonth(shiftYear, shiftMonth);
            DayOfWeek dayOfWeek = new DateTime(shiftYear, shiftMonth, 1).DayOfWeek;

            ShiftMonthViewModel model = new ShiftMonthViewModel()
            {
                StartDate = new DateTime(shiftYear, shiftMonth, 1),
                shiftDetails = query,
                DaysInMonth = days,
            };

            return PartialView("Schedule/Partial/_MonthViewPartial", model);
        }

        #endregion

        public IActionResult Invoicing()
        {
            return View("Header/Invoicing");
        }

        public IActionResult Logout()
        {

            Response.Cookies.Delete("hallodoc");
            TempData["success"] = "Logout Successfull";

            return Redirect("/Guest/Login");
        }

        #region Dashboard

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult ViewNotes(int requestId)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            ViewNotesViewModel? model = _physicianService.AdminProviderService.GetViewNotesModel(requestId);

            if (model == null)
            {
                _notyf.Error("Cannot get data. Please try again later.");
                return RedirectToAction("Dashboard");
            }

            model.UserName = phyName;
            model.IsAdmin = false;


            return View("AdminProvider/ViewNotes", model);
        }

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        [HttpPost]
        public IActionResult ViewNotes(ViewNotesViewModel model)
        {

            try
            {
                string? phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

                if (phyAspId == null)
                {
                    _notyf.Error("Cannot get user id. Please try again later.");
                    return RedirectToAction("Dashboard");
                }

                ServiceResponse response = _physicianService.AdminProviderService.SubmitViewNotes(model, phyAspId, false);

                if (response.StatusCode == ResponseCode.Success)
                {
                    _notyf.Success("Notes Updated Successfully.");
                    return RedirectToAction("ViewNotes", new { requestId = model.RequestId });
                }

                _notyf.Error(response.Message);
                return View("AdminProvider/ViewNotes", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult ViewCase(int? requestId)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            if (requestId == null)
            {
                return View("Error");
            }

            ViewCaseViewModel? model = _physicianService.AdminProviderService.GetViewCaseModel(requestId ?? 0);

            if (model == null)
            {
                _notyf.Error("Cannot get data. Please try again later.");
                return RedirectToAction("Dashboard");
            }

            model.UserName = phyName;
            model.IsAdmin = false;

            return View("AdminProvider/ViewCase", model);

        }

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        [HttpPost]
        public async Task<ActionResult> PartialTable(int status, int page, int typeFilter, string searchFilter, int regionFilter)
        {

            HttpContext.Session.SetInt32("currentStatus", status);
            HttpContext.Session.SetInt32("currentPage", page);
            HttpContext.Session.SetInt32("currentTypeFilter", typeFilter);
            HttpContext.Session.SetInt32("currentRegionFilter", regionFilter);
            HttpContext.Session.SetString("currentSearchFilter", searchFilter ?? "");

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

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

            PagedList<PhyDashboardTRow> pagedList = await _physicianService.PhysicianDashboardService.GetPhysicianRequestAsync(filter, phyId);

            PhysicianDashboardViewModel model = new PhysicianDashboardViewModel();
            model.pagedList = pagedList;
            model.DashboardStatus = status;
            model.CurrentPage = pageNumber;
            model.filterOptions = filter;

            return PartialView("Partial/PartialTable", model);
        }


        public IActionResult ViewUploads(int requestId)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
            if (req == null)
            {
                return View("Error");
            }

            Requestclient? reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == req.Requestid);
            if (reqCli == null)
            {
                return View("Error");
            }

            List<Requestwisefile> files = _unitOfWork.RequestWiseFileRepository.Where(file => file.Requestid == requestId && file.Isdeleted != true).ToList();

            List<string> ext = new List<string>();
            for (int i = 0; i < files.Count; i++)
            {
                ext.Add(Path.GetExtension(files[i].Filename));
            }

            ViewUploadsViewModel model = new ViewUploadsViewModel()
            {
                PatientName = reqCli.Firstname + " " + reqCli.Lastname,
                requestwisefiles = files,
                ConfirmationNumber = req.Confirmationnumber,
                RequestId = req.Requestid,
                extensions = ext,
                UserName = adminName,
                RequestFilesPath = REQUEST_FILE_PATH,
            };
            
            return View("Dashboard/ViewUploads", model);
        }

        [HttpPost]
        public bool DeleteAllFiles(int requestId)
        {
            try
            {
                IEnumerable<Requestwisefile> files = _unitOfWork.RequestWiseFileRepository.Where(file => file.Requestid == requestId);

                foreach (Requestwisefile file in files)
                {
                    file.Isdeleted = true;
                    _unitOfWork.RequestWiseFileRepository.Update(file);
                }

                _unitOfWork.Save();

                TempData["success"] = "Files deleted Succesfully.";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error occured while deleting files.";
                return false;
            }
        }

        [HttpPost]
        public bool DeleteFile(int requestWiseFileId)
        {
            try
            {
                Requestwisefile file = _unitOfWork.RequestWiseFileRepository.GetFirstOrDefault(reqFile => reqFile.Requestwisefileid == requestWiseFileId);

                file.Isdeleted = true;
                _unitOfWork.RequestWiseFileRepository.Update(file);
                _unitOfWork.Save();

                TempData["success"] = "File deleted Succesfully.";
                return true;
            }
            catch (Exception e)
            {
                TempData["error"] = "Error occured while deleting file.";
                return false;
            }

        }


        private DateTime GetNextWeekday(DateTime startDate, int targetDayOfWeek)
        {
            int currentDayOfWeek = (int)startDate.DayOfWeek;
            int daysToAdd = targetDayOfWeek - currentDayOfWeek;

            if (daysToAdd <= 0) daysToAdd += 7; // If the target day is earlier in the week, move to next week

            return startDate.AddDays(daysToAdd);
        }



        public int GetOffSet(int currentDay, List<int> repeatDays)
        {
            int nextVal = repeatDays.SkipWhile(day => day <= currentDay).FirstOrDefault();
            int index = repeatDays.IndexOf(nextVal);
            return index;
        }


        [HttpPost]
        public IActionResult AddShift(SchedulingViewModel model)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            string phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

            if (ModelState.IsValid)
            {
                Shift shift = new Shift();
                shift.Physicianid = (int)model.addShiftPhysician;
                shift.Startdate = DateOnly.FromDateTime(model.shiftDate.Value.Date);
                if (model.isRepeat != null)
                {
                    shift.Isrepeat = true;
                }
                else
                {
                    shift.Isrepeat = false;
                }

                shift.Repeatupto = model.repeatCount;
                shift.Createddate = DateTime.Now;
                shift.Createdby = phyAspId;
                _unitOfWork.ShiftRepository.Add(shift);
                _unitOfWork.Save();

                Shiftdetail shiftdetail1 = new()
                {
                    Shiftid = shift.Shiftid,
                    Shiftdate = (DateTime)model.shiftDate,
                    Regionid = model.addShiftRegion,
                    Starttime = (TimeOnly)model.shiftStartTime,
                    Endtime = (TimeOnly)model.shiftEndTime,
                    Status = (short)ShiftStatus.Pending,
                    Isdeleted = false
                };

                _unitOfWork.ShiftDetailRepository.Add(shiftdetail1);
                _unitOfWork.Save();

                if (model.isRepeat != null)
                {

                    DateTime currentDate = (DateTime)model.shiftDate;
                    int currentDayOfWeek = (int)model.shiftDate.Value.DayOfWeek;

                    int offset = GetOffSet(currentDayOfWeek, model.repeatDays);

                    for (int i = 0; i < model.repeatCount; i++)
                    {
                        int length = model.repeatDays.Count;
                        for (int j = 0; j < length; j++)
                        {
                            int offsetIndex = (j + offset) % length;

                            DateTime nextShiftDate = GetNextWeekday(currentDate, model.repeatDays[offsetIndex]);
                            Shiftdetail shiftdetail = new()
                            {
                                Shiftid = shift.Shiftid,
                                Shiftdate = nextShiftDate,
                                Regionid = model.addShiftRegion,
                                Starttime = (TimeOnly)model.shiftStartTime,
                                Endtime = (TimeOnly)model.shiftEndTime,
                                Status = (short)ShiftStatus.Pending,
                                Isdeleted = false
                            };

                            _unitOfWork.ShiftDetailRepository.Add(shiftdetail);
                            _unitOfWork.Save();

                            Shiftdetailregion s = new()
                            {
                                Shiftdetailid = shiftdetail.Shiftdetailid,
                                Regionid = (int)shiftdetail.Regionid
                            };
                            _unitOfWork.ShiftDetailRegionRepository.Add(s);
                            _unitOfWork.Save();
                        }

                        currentDate = GetNextWeekday(currentDate, 7); // Move to next week
                    }

                }

            }

            return RedirectToAction("Schedule");
        }



        [HttpPost]
        public IActionResult ViewUploads(ViewUploadsViewModel uploadsVM)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (uploadsVM.File != null)
            {
                string webRootPath = _environment.WebRootPath;
                string requestPath = Path.Combine(webRootPath, REQUEST_FILE_PATH, uploadsVM.RequestId.ToString());
                FileHelper.InsertFileAtPath(uploadsVM.File, requestPath);

                Requestwisefile requestwisefile = new()
                {
                    Requestid = uploadsVM.RequestId,
                    Filename = uploadsVM.File.FileName,
                    Createddate = DateTime.Now,
                    Physicianid = phyId,
                    Ip = "127.0.0.1",
                };

                _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                _unitOfWork.Save();

                uploadsVM.File = null;

            }

            return RedirectToAction("ViewUploads", new { Requestid = uploadsVM.RequestId });
        }



        public IActionResult Dashboard()
        {
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (phyName == null)
            {
                return RedirectToAction("Index", "Guest");
            }

            int? status = HttpContext.Session.GetInt32("currentStatus");
            int? page = HttpContext.Session.GetInt32("currentPage");
            int? region = HttpContext.Session.GetInt32("currentRegionFilter");
            int? type = HttpContext.Session.GetInt32("currentTypeFilter");
            string? search = HttpContext.Session.GetString("currentSearchFilter");


            if (status == null)
            {
                status = 1;
                HttpContext.Session.SetInt32("currentStatus", 1);
            }
            if (page == null)
            {
                page = 1;
                HttpContext.Session.SetInt32("currentPage", 1);
            }
            if (region == null)
            {
                region = 0;
                HttpContext.Session.SetInt32("currentRegionFilter", 0);
            }
            if (type == null)
            {
                type = 0;
                HttpContext.Session.SetInt32("currentTypeFilter", 0);
            }
            if (search == null)
            {
                search = "";
                HttpContext.Session.SetString("currentSearchFilter", "");
            }

            DashboardFilter initialFilter = new DashboardFilter();
            initialFilter.status = (int)status;
            initialFilter.pageNumber = (int)page;
            initialFilter.RegionFilter = (int)region;
            initialFilter.RequestTypeFilter = (int)type;
            initialFilter.PatientSearchText = (string)search;

            PhysicianDashboardViewModel model = new PhysicianDashboardViewModel();
            model.UserName = phyName;
            model.physicians = _unitOfWork.PhysicianRepository.GetAll();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.NewReqCount = _unitOfWork.RequestRepository.Count(r => r.Physicianid == phyId && r.Status == (short)RequestStatus.Unassigned);
            model.PendingReqCount = _unitOfWork.RequestRepository.Count(r => r.Physicianid == phyId && r.Status == (short)RequestStatus.Accepted);
            model.ActiveReqCount = _unitOfWork.RequestRepository.Count(r => r.Physicianid == phyId && ((r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite)));
            model.ConcludeReqCount = _unitOfWork.RequestRepository.Count(r => r.Physicianid == phyId && r.Status == (short)RequestStatus.Conclude);
            model.casetags = _unitOfWork.CaseTagRepository.GetAll();
            model.filterOptions = initialFilter;

            return View("Header/Dashboard", model);

        }

        public async Task<string> GetAddressFromLatLng(double latitude, double longtitude)
        {
            try
            {

                using (var client = new HttpClient())
                {
                    string apiKey = _config.GetSection("Geocoding")["ApiKey"];
                    string baseUrl = $"https://geocode.maps.co/reverse?lat={latitude}&lon={longtitude}&api_key=" + apiKey;
                    //HTTP GET

                    var responseTask = client.GetAsync(baseUrl);
                    responseTask.Wait();

                    var result = responseTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        var content = await result.Content.ReadAsStringAsync();

                        var json = JsonObject.Parse(content);

                        string address = json?["display_name"]?.ToString() ?? "";

                        return address;
                    }
                    else
                    {
                        //log response status here

                        ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");

                        return "";
                    }
                }

            }
            catch (Exception e)
            {
                var error = e.Message;
                return "";
            }

        }

        [HttpPost]
        public async Task<bool> UpdatePhysicianLocation(double latitude, double longitude)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            try
            {
                Physician physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == phyId);
                Physicianlocation phyLocation = _unitOfWork.PhysicianLocationRepo.GetFirstOrDefault(loc => loc.Physicianid == phyId);
                string latLngAddress = await GetAddressFromLatLng(latitude, longitude);


                if (phyLocation == null)
                {

                    phyLocation = new Physicianlocation()
                    {
                        Physicianid = phyId,
                        Latitude = latitude,
                        Longitude = longitude,
                        Createddate = DateTime.Now,
                        Physicianname = physician.Firstname + " " + physician.Lastname,
                        Address = latLngAddress,
                    };

                    _unitOfWork.PhysicianLocationRepo.Add(phyLocation);
                    _unitOfWork.Save();

                }
                else
                {
                    phyLocation.Latitude = latitude;
                    phyLocation.Longitude = longitude;
                    phyLocation.Createddate = DateTime.Now;
                    phyLocation.Address = latLngAddress;

                    _unitOfWork.PhysicianLocationRepo.Update(phyLocation);
                    _unitOfWork.Save();

                }


                return true;
            }

            catch (Exception ex)
            {
                return false;
            }

        }

        [HttpPost]
        public bool AcceptCase(int requestId)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                DateTime currentTime = DateTime.Now;
                Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (req == null)
                {
                    _notyf.Error("Cannot find request. Please try again later.");
                    return false;
                }

                req.Accepteddate = currentTime;
                req.Status = (int)RequestStatus.Accepted;
                req.Modifieddate = currentTime;


                _unitOfWork.RequestRepository.Update(req);

                string logNotes = phyName + " accepted request on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss");

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestId,
                    Status = (short)RequestStatus.Accepted,
                    Physicianid = phyId,
                    Notes = logNotes,
                    Transtophysicianid = req.Physicianid,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);

                _unitOfWork.Save();

                _notyf.Success("Request Accepted Successfully.");

                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        [HttpPost]
        public bool TransferCaseModal(int requestId, string description)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (req == null)
                {
                    _notyf.Error("Cannot find request. Please try again later.");
                    return false;
                }
                DateTime currentTime = DateTime.Now;

                req.Accepteddate = null;
                req.Physicianid = null;
                req.Status = (int)RequestStatus.Unassigned;
                req.Modifieddate = currentTime;

                _unitOfWork.RequestRepository.Update(req);


                string logNotes = phyName + " transferred request to Admin on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + description;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestId,
                    Status = (short)RequestStatus.Unassigned,
                    Physicianid = phyId,
                    Notes = logNotes,
                    Createddate = currentTime,
                    Transtoadmin = true,
                };
                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);

                _unitOfWork.Save();

                _notyf.Success("Request Successfully transferred to admin.");

                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }



        [HttpPost]
        public IActionResult SendAgreementMail(SendAgreementModel model)
        {
            try
            {
                string encryptedId = EncryptionService.Encrypt(model.RequestId.ToString());
                var agreementLink = Url.Action("ReviewAgreement", "Guest", new { requestId = encryptedId }, Request.Scheme);

                string senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
                string senderPassword = _config.GetSection("OutlookSMTP")["Password"];

                SmtpClient client = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                string subject = "Set up your Account";
                string body = "<h1>Hello , Patient!!</h1><p>You can review your agrrement and accept it to go ahead with the medical process, which  sent by the physician. </p><a href=\"" + agreementLink + "\" >Click here to accept agreement</a>";

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = body,
                };

                mailMessage.To.Add(model.PatientEmail);

                client.Send(mailMessage);

                TempData["success"] = "Agreement Sent Successfully.";
                return Redirect("/Admin/Dashboard");

            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while sending agreement.";
                return Redirect("/Admin/Dashboard");
            }

        }

        [HttpPost]
        public IActionResult DownloadEncounterPdf(int? requestId)
        {
            //string encounterFormHtml = "";
            //using (MemoryStream stream = new System.IO.MemoryStream())
            //{
            //    StringReader sr = new StringReader(encounterFormHtml);
            //    Document pdfDoc = new Document(PageSize.A4, 10f, 10f, 100f, 0f);
            //    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
            //    pdfDoc.Open();
            //    XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
            //    pdfDoc.Close();
            //    return File(stream.ToArray(), "application/pdf", "Grid.pdf");
            //}

            Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(r => r.Requestid == requestId);

            if (request == null)
            {
                _notyf.Error("Cannot Find Request");
                return RedirectToAction("Dashboard");
            }

            Encounterform? oldEncounterForm = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(e => e.Requestid == requestId);
            Requestclient requestclient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(e => e.Requestid == requestId);
            string dobDate = null;


            if (requestclient.Intyear != null && requestclient.Strmonth != null && requestclient.Intdate != null)
            {
                dobDate = requestclient.Intyear + "-" + requestclient.Strmonth + "-" + requestclient.Intdate;
            }

            EncounterFormViewModel encounterViewModel = new EncounterFormViewModel()
            {
                FirstName = requestclient.Firstname,
                LastName = requestclient.Lastname,
                Email = requestclient.Email,
                PhoneNumber = requestclient.Phonenumber,
                DOB = dobDate != null ? DateTime.Parse(dobDate) : null,
                CreatedDate = request.Createddate,
                Location = requestclient.Street + " " + requestclient.City + " " + requestclient.State,
                MedicalHistory = oldEncounterForm.Medicalhistory,
                History = oldEncounterForm.Historyofpresentillnessorinjury,
                Medications = oldEncounterForm.Medications,
                Allergies = oldEncounterForm.Allergies,
                Temp = oldEncounterForm.Temp,
                HR = oldEncounterForm.Hr,
                RR = oldEncounterForm.Rr,
                BpLow = oldEncounterForm.Bloodpressuresystolic,
                BpHigh = oldEncounterForm.Bloodpressuresystolic,
                O2 = oldEncounterForm.O2,
                Pain = oldEncounterForm.Pain,
                Heent = oldEncounterForm.Heent,
                CV = oldEncounterForm.Cv,
                Chest = oldEncounterForm.Chest,
                ABD = oldEncounterForm.Abd,
                Extr = oldEncounterForm.Extremities,
                Skin = oldEncounterForm.Skin,
                Neuro = oldEncounterForm.Neuro,
                Other = oldEncounterForm.Other,
                Diagnosis = oldEncounterForm.Diagnosis,
                TreatmentPlan = oldEncounterForm.TreatmentPlan,
                Procedures = oldEncounterForm.Procedures,
                MedicationDispensed = oldEncounterForm.Medicaldispensed,
                FollowUps = oldEncounterForm.Followup

            };

            return new ViewAsPdf("Partial/PdfPartial", encounterViewModel)
            {
                FileName = "FinalizedEncounterForm.pdf"
            };
        }

        [HttpPost]
        public bool EncounterHouseCallBegin(int requestId)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (request == null)
                {
                    _notyf.Error("Cannot find request. Please try again later.");
                    return false;
                }

                DateTime currentTime = DateTime.Now;

                request.Status = (int)RequestStatus.MDOnSite;
                request.Modifieddate = currentTime;
                request.Calltype = (int)RequestCallType.HouseCall;


                _unitOfWork.RequestRepository.Update(request);

                string logNotes = phyName + " started house call encounter on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss");

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestId,
                    Status = (short)RequestStatus.MDOnSite,
                    Physicianid = phyId,
                    Notes = logNotes,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);

                _unitOfWork.Save();

                _notyf.Success("Successfully Started House Call Consultation.");

                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        [HttpPost]
        public bool EncounterHouseCallFinish(int requestId)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (request == null)
                {
                    _notyf.Error("Cannot find request. Please try again later.");
                    return false;
                }

                DateTime currentTime = DateTime.Now;

                request.Status = (int)RequestStatus.Conclude;
                request.Modifieddate = currentTime;
                request.Calltype = (int)RequestCallType.HouseCall;

                _unitOfWork.RequestRepository.Update(request);

                string logNotes = phyName + " finished house call encounter on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss");

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestId,
                    Status = (short)RequestStatus.Conclude,
                    Physicianid = phyId,
                    Notes = logNotes,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);

                _unitOfWork.Save();

                _notyf.Success("Successfully Started House Call Consultation.");

                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        [HttpPost]
        public bool EncounterConsult(int requestId)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);
                if (request == null)
                {
                    _notyf.Error("Cannot find request. Please try again later.");
                    return false;
                }

                DateTime currentTime = DateTime.Now;

                request.Status = (int)RequestStatus.Conclude;
                request.Modifieddate = currentTime;
                request.Calltype = (int)RequestCallType.HouseCall;

                _unitOfWork.RequestRepository.Update(request);

                string logNotes = phyName + " consulted the request on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss");

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestId,
                    Status = (short)RequestStatus.Conclude,
                    Physicianid = phyId,
                    Notes = logNotes,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);

                _unitOfWork.Save();

                _notyf.Success("Successfully Consulted Request.");

                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        public IActionResult ConcludeCare(int requestId)
        {
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            Requestclient? client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(cli => cli.Requestid == requestId);

            ConcludeCareViewModel model = new ConcludeCareViewModel()
            {
                RequestId = requestId,
                UserName = phyName,
                PatientName = client?.Firstname + " " + client?.Lastname,
                fileNames = _unitOfWork.RequestWiseFileRepository.GetAll().Select(_ => _.Filename),
            };

            return View("Dashboard/ConcludeCare", model);
        }

        public IActionResult ConcludeCasePhysician(int requestId, string phyNotes)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestId);

                if (request == null)
                {
                    _notyf.Error("Cannot find request. Please try again later.");
                    return RedirectToAction("ConcludeCare", new { requestId = requestId });
                }

                DateTime currentTime = DateTime.Now;

                request.Status = (int)RequestStatus.Closed;
                request.Completedbyphysician = true;
                request.Modifieddate = currentTime;

                _unitOfWork.RequestRepository.Update(request);

                string logNotes = phyName + " concluded the request on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + phyNotes;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestId,
                    Status = (short)RequestStatus.Closed,
                    Physicianid = phyId,
                    Notes = logNotes,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);

                _unitOfWork.Save();

                _notyf.Success("Successfully Consulted Request.");

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("ConcludeCare", new { requestId = requestId });
            }

        }

        public IActionResult EncounterForm(int requestid)
        {
            Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(r => r.Requestid == requestid);

            if (request == null)
            {
                _notyf.Error("Cannot Find Request");
                return RedirectToAction("Dashboard");
            }

            Encounterform? oldEncounterForm = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(e => e.Requestid == requestid);
            Requestclient requestclient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(e => e.Requestid == requestid);
            string dobDate = null;


            if (requestclient.Intyear != null && requestclient.Strmonth != null && requestclient.Intdate != null)
            {
                dobDate = requestclient.Intyear + "-" + requestclient.Strmonth + "-" + requestclient.Intdate;
            }

            EncounterFormViewModel encounterViewModel;

            if (oldEncounterForm != null)
            {

                if (oldEncounterForm.Isfinalize)
                {
                    _notyf.Error("Form is Already Finalized");
                    return RedirectToAction("Dashboard");
                }

                encounterViewModel = new()
                {
                    RequestId = requestid,
                    FirstName = requestclient.Firstname,
                    LastName = requestclient.Lastname,
                    Email = requestclient.Email,
                    PhoneNumber = requestclient.Phonenumber,
                    DOB = dobDate != null ? DateTime.Parse(dobDate) : null,
                    CreatedDate = request.Createddate,
                    Location = requestclient.Street + " " + requestclient.City + " " + requestclient.State,
                    MedicalHistory = oldEncounterForm.Medicalhistory,
                    History = oldEncounterForm.Historyofpresentillnessorinjury,
                    Medications = oldEncounterForm.Medications,
                    Allergies = oldEncounterForm.Allergies,
                    Temp = oldEncounterForm.Temp,
                    HR = oldEncounterForm.Hr,
                    RR = oldEncounterForm.Rr,
                    BpLow = oldEncounterForm.Bloodpressuresystolic,
                    BpHigh = oldEncounterForm.Bloodpressuresystolic,
                    O2 = oldEncounterForm.O2,
                    Pain = oldEncounterForm.Pain,
                    Heent = oldEncounterForm.Heent,
                    CV = oldEncounterForm.Cv,
                    Chest = oldEncounterForm.Chest,
                    ABD = oldEncounterForm.Abd,
                    Extr = oldEncounterForm.Extremities,
                    Skin = oldEncounterForm.Skin,
                    Neuro = oldEncounterForm.Neuro,
                    Other = oldEncounterForm.Other,
                    Diagnosis = oldEncounterForm.Diagnosis,
                    TreatmentPlan = oldEncounterForm.TreatmentPlan,
                    Procedures = oldEncounterForm.Procedures,
                    MedicationDispensed = oldEncounterForm.Medicaldispensed,
                    FollowUps = oldEncounterForm.Followup

                };

                return View("Dashboard/EncounterForm", encounterViewModel);

            }

            encounterViewModel = new()
            {
                RequestId = requestid,
                FirstName = requestclient.Firstname,
                LastName = requestclient.Lastname,
                Email = requestclient.Email,
                PhoneNumber = requestclient.Phonenumber,
                DOB = dobDate != null ? DateTime.Parse(dobDate) : null,
                CreatedDate = request.Createddate,
                Location = requestclient.Street + " " + requestclient.City + " " + requestclient.State,
            };

            return View("Dashboard/EncounterForm", encounterViewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EncounterForm(EncounterFormViewModel model)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            Request? r = _unitOfWork.RequestRepository.GetFirstOrDefault(rs => rs.Requestid == model.RequestId);
            if (ModelState.IsValid)
            {
                Encounterform? oldEncounterForm = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(e => e.Requestid == model.RequestId);

                if (oldEncounterForm == null)
                {
                    Encounterform newEncounterForm = new()
                    {
                        Requestid = model.RequestId,
                        Historyofpresentillnessorinjury = model.History,
                        Medicalhistory = model.MedicalHistory,
                        Medications = model.Medications,
                        Allergies = model.Allergies,
                        Temp = model.Temp,
                        Hr = model.HR,
                        Rr = model.RR,
                        Bloodpressuresystolic = model.BpLow,
                        Bloodpressurediastolic = model.BpHigh,
                        O2 = model.O2,
                        Pain = model.Pain,
                        Skin = model.Skin,
                        Heent = model.Heent,
                        Neuro = model.Neuro,
                        Other = model.Other,
                        Cv = model.CV,
                        Chest = model.Chest,
                        Abd = model.ABD,
                        Extremities = model.Extr,
                        Diagnosis = model.Diagnosis,
                        TreatmentPlan = model.TreatmentPlan,
                        Procedures = model.Procedures,
                        Physicianid = phyId,
                        Isfinalize = false,
                    };

                    _unitOfWork.EncounterFormRepository.Add(newEncounterForm);
                    _unitOfWork.Save();

                }
                else
                {
                    oldEncounterForm.Requestid = model.RequestId;
                    oldEncounterForm.Historyofpresentillnessorinjury = model.History;
                    oldEncounterForm.Medicalhistory = model.MedicalHistory;
                    oldEncounterForm.Medications = model.Medications;
                    oldEncounterForm.Allergies = model.Allergies;
                    oldEncounterForm.Temp = model.Temp;
                    oldEncounterForm.Hr = model.HR;
                    oldEncounterForm.Rr = model.RR;
                    oldEncounterForm.Bloodpressuresystolic = model.BpLow;
                    oldEncounterForm.Bloodpressurediastolic = model.BpHigh;
                    oldEncounterForm.O2 = model.O2;
                    oldEncounterForm.Pain = model.Pain;
                    oldEncounterForm.Skin = model.Skin;
                    oldEncounterForm.Heent = model.Heent;
                    oldEncounterForm.Neuro = model.Neuro;
                    oldEncounterForm.Other = model.Other;
                    oldEncounterForm.Cv = model.CV;
                    oldEncounterForm.Chest = model.Chest;
                    oldEncounterForm.Abd = model.ABD;
                    oldEncounterForm.Extremities = model.Extr;
                    oldEncounterForm.Diagnosis = model.Diagnosis;
                    oldEncounterForm.TreatmentPlan = model.TreatmentPlan;
                    oldEncounterForm.Procedures = model.Procedures;
                    oldEncounterForm.Physicianid = phyId;
                    oldEncounterForm.Isfinalize = false;

                    _unitOfWork.EncounterFormRepository.Update(oldEncounterForm);
                    _unitOfWork.Save();
                }
                return RedirectToAction("EncounterForm", new { requestid = model.RequestId });
            }

            return View("error");
        }

        public bool FinalizeEncounterForm(int requestId)
        {
            try
            {
                Encounterform? form = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(form => form.Requestid == requestId);

                if (form == null)
                {
                    _notyf.Error("No previous form have been saved.");
                    return false;
                }

                form.Isfinalize = true;

                _unitOfWork.EncounterFormRepository.Update(form);
                _unitOfWork.Save();

                _notyf.Success("Form Successfully Finalized");
                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        public IActionResult CreateRequest()
        {
            IEnumerable<Region> regions = _unitOfWork.RegionRepository.GetAll();
            AdminCreateRequestViewModel model = new AdminCreateRequestViewModel();
            model.regions = regions;
            return View("AdminProvider/CreateRequest", model);
        }

        [HttpPost]
        public IActionResult CreateRequest(AdminCreateRequestViewModel model)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            string? phyAspId = HttpContext.Request.Headers.Where(a => a.Key == "userAspId").FirstOrDefault().Value;

            if (phyId == 0 || string.IsNullOrEmpty(phyAspId))
            {
                _notyf.Error("Couldn't get admin data.");
                return RedirectToAction("Dashboard");
            }

            try
            {

                if (ModelState.IsValid)
                {
                    string? createLink = Url.Action("CreateAccount", "Guest", null, Request.Scheme);

                    ServiceResponse response = _physicianService.AdminProviderService.SubmitCreateRequest(model, phyAspId, createLink ?? "",false);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success("Request Created Successfully");
                        return RedirectToAction("Dashboard");
                    }

                    _notyf.Error(response.Message);
                    return View("AdminProvider/CreateRequest", model);
                }

                _notyf.Error("Please enter all the requried fields");
                return View("AdminProvider/CreateRequest", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("AdminProvider/CreateRequest", model);
            }

        }


        public void SendMailForCreateAccount(string email)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "userId").Value);

            try
            {
                Aspnetuser aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(user => user.Email == email);

                string createAccToken = Guid.NewGuid().ToString();

                Passtoken passtoken = new Passtoken()
                {
                    Aspnetuserid = aspUser.Id,
                    Createddate = DateTime.Now,
                    Email = email,
                    Isdeleted = false,
                    Isresettoken = false,
                    Uniquetoken = createAccToken,
                };

                _unitOfWork.PassTokenRepository.Add(passtoken);
                _unitOfWork.Save();

                var createLink = Url.Action("CreateAccount", "Guest", new { token = createAccToken }, Request.Scheme);
                string subject = "Set up your Account";
                string body = "<h1>Create Account By clicking below</h1><a href=\"" + createLink + "\" >Create Account link</a>";


                _emailService.SendMail(email, body, subject, out int sentTries, out bool isSent);

                Emaillog emailLog = new Emaillog()
                {
                    Emailtemplate = "1",
                    Subjectname = subject,
                    Emailid = email,
                    Roleid = (int)AccountType.Patient,
                    Adminid = adminId,
                    Createdate = DateTime.Now,
                    Sentdate = DateTime.Now,
                    Isemailsent = isSent,
                    Senttries = sentTries,
                };

                _unitOfWork.EmailLogRepository.Add(emailLog);
                _unitOfWork.Save();

                TempData["success"] = "Email has been successfully sent to " + email + " for create account link.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
        }

        public IActionResult Orders(int requestId)
        {

            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            SendOrderViewModel model = new SendOrderViewModel();
            model.professionalTypeList = _unitOfWork.HealthProfessionalTypeRepo.GetAll();
            model.RequestId = requestId;
            model.UserName = adminName;

            return View("Dashboard/Orders", model);
        }

        [HttpPost]
        public IActionResult Orders(SendOrderViewModel orderViewModel)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            Physician phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(ad => ad.Physicianid == phyId);

            if (ModelState.IsValid)
            {

                Orderdetail order = new Orderdetail()
                {
                    Vendorid = orderViewModel.SelectedVendor,
                    Requestid = orderViewModel.RequestId,
                    Faxnumber = orderViewModel.FaxNumber,
                    Email = orderViewModel.Email,
                    Businesscontact = orderViewModel.BusinessContact,
                    Prescription = orderViewModel.Prescription,
                    Noofrefill = orderViewModel.NoOfRefills,
                    Createddate = DateTime.Now,
                    Createdby = phy.Aspnetuserid,
                };

                _unitOfWork.OrderDetailRepo.Add(order);
                _unitOfWork.Save();

                TempData["success"] = "Order Successfully Sent";

            }
            else
            {
                TempData["error"] = "Error occured whlie ordering.";
            }

            return RedirectToAction("Dashboard");
        }


        [HttpPost]
        public IActionResult SendLinkForSubmitRequest(SendLinkModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            if (ModelState.IsValid)
            {
                try
                {
                    string? sendPatientLink = Url.Action("SubmitRequest", "Guest", new { }, Request.Scheme);

                    string? senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
                    string? senderPassword = _config.GetSection("OutlookSMTP")["Password"];

                    SmtpClient client = new SmtpClient("smtp.office365.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(senderEmail, senderPassword),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false
                    };

                    string subject = "Set up your Account";
                    string body = "<h1>Hola , " + model.FirstName + " " + model.LastName + "!!</h1><p>Clink the link below to create request.</p><a href=\"" + sendPatientLink + "\" >Submit Request Link</a>";

                    if (senderEmail != null)
                    {

                        _emailService.SendMail(model.Email, body, subject, out int sentTries, out bool isSent);

                        Emaillog emailLog = new Emaillog()
                        {
                            Emailtemplate = "1",
                            Subjectname = subject,
                            Emailid = model.Email,
                            Roleid = (int)AccountType.Patient,
                            Physicianid = adminId,
                            Createdate = DateTime.Now,
                            Sentdate = DateTime.Now,
                            Isemailsent = isSent,
                            Senttries = sentTries,
                        };

                        _unitOfWork.EmailLogRepository.Add(emailLog);
                        _unitOfWork.Save();

                    }

                    Smslog smsLog = new Smslog()
                    {
                        Smstemplate = "1",
                        Mobilenumber = model.Phone,
                        Roleid = (int)AccountType.Patient,
                        Physicianid = adminId,
                        Createdate = DateTime.Now,
                        Sentdate = DateTime.Now,
                        Issmssent = true,
                        Senttries = 1,
                    };

                    _unitOfWork.SMSLogRepository.Add(smsLog);
                    _unitOfWork.Save();

                    return Redirect("/Physician/Dashboard");
                }
                catch (Exception e)
                {
                    TempData["error"] = "Error occurred : " + e.Message;
                    return Redirect("/Physician/Dashboard");
                }
            }

            TempData["error"] = "Please Fill all details for sending link.";
            return Redirect("/Physician/Dashboard");
        }


        #endregion


        #region HelperFunctions

        public string GenerateConfirmationNumber(User user)
        {
            string regionAbbr = _unitOfWork.RegionRepository.GetFirstOrDefault(region => region.Regionid == user.Regionid).Abbreviation;

            DateTime todayStart = DateTime.Now.Date;
            int count = _unitOfWork.RequestRepository.Count(req => req.Createddate > todayStart);

            string confirmationNumber = regionAbbr + user.Createddate.Date.ToString("D2") + user.Createddate.Month.ToString("D2") + user.Lastname.Substring(0, 2).ToUpper() + user.Firstname.Substring(0, 2).ToUpper() + (count + 1).ToString("D4");
            return confirmationNumber;
        }


        [HttpPost]
        public JsonArray GetBusinessByType(int professionType)
        {
            var result = new JsonArray();
            IEnumerable<Healthprofessional> businesses = _unitOfWork.HealthProfessionalRepo.Where(prof => prof.Profession == professionType);

            foreach (Healthprofessional business in businesses)
            {
                result.Add(new { businessId = business.Vendorid, businessName = business.Vendorname });
            }

            return result;
        }

        [HttpPost]
        public JsonArray GetPhysicianByRegion(int regionId)
        {
            var result = new JsonArray();
            IEnumerable<Physician> physicians = _unitOfWork.PhysicianRepository.Where(phy => phy.Regionid == regionId);

            foreach (Physician physician in physicians)
            {
                result.Add(new { physicianId = physician.Physicianid, physicianName = physician.Firstname + " " + physician.Lastname });
            }

            return result;
        }


        [HttpPost]
        public Healthprofessional GetBusinessDetailsById(int vendorId)
        {
            if (vendorId <= 0)
            {
                return null;
            }
            Healthprofessional business = _unitOfWork.HealthProfessionalRepo.GetFirstOrDefault(prof => prof.Vendorid == vendorId);

            return business;
        }


        #endregion
    }
}
