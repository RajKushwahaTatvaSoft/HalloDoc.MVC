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
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Services.AdminServices;
using Business_Layer.Services.PhysicianServices.Interface;
using System.IO.Compression;
using Data_Layer.CustomModels.Filter;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

        #region Header

        public IActionResult Logout()
        {

            Response.Cookies.Delete("hallodoc");
            TempData["success"] = "Logout Successfull";

            return Redirect("/Guest/Login");
        }

        #endregion

        #region Profile


        [RoleAuthorize((int)AllowMenu.ProviderProfile)]
        public IActionResult Profile()
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

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
                PhyUserName = aspUser.Username,
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

                if (aspUser == null)
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
            string phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

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

        [RoleAuthorize((int)AllowMenu.ProviderSchedule)]
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

        [RoleAuthorize((int)AllowMenu.ProviderSchedule)]
        public IActionResult Schedule()
        {

            SchedulingViewModel model = new SchedulingViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();

            return View("Schedule/MySchedule", model);
        }

        [RoleAuthorize((int)AllowMenu.ProviderSchedule)]
        public IActionResult LoadMonthSchedule(int shiftMonth, int shiftYear)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (phyId == 0)
            {
                return View("ErrorPartial");
            }

            // 0 index'ed month of js to 1 index'ed month of c#
            shiftMonth++;

            IEnumerable<Shiftdetail> query = (from shift in _unitOfWork.ShiftRepository.GetAll()
                                              where (shift.Physicianid == phyId)
                                              join shiftDetail in _unitOfWork.ShiftDetailRepository.GetAll()
                                              on shift.Shiftid equals shiftDetail.Shiftid
                                              where (shiftDetail.Shiftdate.Month == shiftMonth && shiftDetail.Shiftdate.Year == shiftYear)
                                              select shiftDetail
                                              );

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

            if (index < 0) index = 0;
            return index;
        }

        public IActionResult ViewShiftModal(int shiftDetailId)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            Shiftdetail? shiftdetail = _unitOfWork.ShiftDetailRepository.GetFirstOrDefault(shift => shift.Shiftdetailid == shiftDetailId);
            if (shiftdetail == null)
            {
                return View("Error");
            }

            Physician? phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == phyId);

            ViewShiftModel model = new ViewShiftModel()
            {
                PhysicianName = phy?.Firstname + " " + phy?.Lastname,
                RegionName = _unitOfWork.RegionRepository.GetFirstOrDefault(reg => reg.Regionid == shiftdetail.Regionid)?.Name,
                ShiftDate = shiftdetail.Shiftdate,
                ShiftEndTime = shiftdetail.Endtime,
                ShiftStartTime = shiftdetail.Starttime,
            };

            return PartialView("Schedule/ViewShiftModal", model);

        }

        [HttpGet]
        public IActionResult AddShiftModal()
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            IEnumerable<int> phyRegions = _unitOfWork.PhysicianRegionRepo.Where(phy => phy.Physicianid == phyId).Select(_ => _.Regionid);
            AddShiftModel model = new AddShiftModel();
            model.regions = _unitOfWork.RegionRepository.Where(reg => phyRegions.Contains(reg.Regionid));
            model.PhysicianId = phyId;
            return PartialView("Schedule/AddShiftModal", model);
        }


        [HttpPost]
        public bool DeleteShift(int shiftDetailId)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            try
            {
                Shiftdetail? sd = _unitOfWork.ShiftDetailRepository.GetFirstOrDefault(s => s.Shiftdetailid == shiftDetailId);


                if (sd == null)
                {
                    TempData["error"] = "Cannot Find Shift";
                    return false;
                }

                DateTime shiftTime = sd.Shiftdate.Date.Add(sd.Starttime.ToTimeSpan());
                if (shiftTime <= DateTime.Now)
                {
                    _notyf.Error("Only future shifts are allowed to be deleted");
                    return false;
                }

                sd.Isdeleted = true;
                _unitOfWork.ShiftDetailRepository.Update(sd);
                _unitOfWork.Save();

                return true;
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return false;
            }

        }


        [HttpPost]
        public bool EditShift(ViewShiftModel model)
        {
            string? phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

            try
            {
                if (ModelState.IsValid)
                {

                    Shiftdetail? sd = _unitOfWork.ShiftDetailRepository.GetFirstOrDefault(s => s.Shiftdetailid == model.ShiftDetailId);
                    if (sd == null)
                    {
                        _notyf.Error("Cannot Find shift");
                        return false;
                    }

                    sd.Starttime = model.ShiftStartTime;
                    sd.Endtime = model.ShiftEndTime;
                    sd.Shiftdate = model.ShiftDate;
                    sd.Modifieddate = DateTime.Now;
                    sd.Modifiedby = phyAspId;
                    sd.Status = (int)ShiftStatus.Pending;

                    _unitOfWork.ShiftDetailRepository.Update(sd);
                    _unitOfWork.Save();

                    _notyf.Success("Shift Edited Successfully");

                    return true;
                }

                IEnumerable<ModelStateEntry>? invalidValues = ModelState.Values.Where(val => val.ValidationState == ModelValidationState.Invalid);
                foreach (var invalid in invalidValues)
                {
                    _notyf.Error(invalid.Errors.FirstOrDefault()?.ErrorMessage);
                }
                return false;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }


        [HttpPost]
        public bool AddShift(AddShiftModel model)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            string? phyAspId = HttpContext.Request.Headers.Where(a => a.Key == "userAspId").FirstOrDefault().Value;

            if (phyAspId == null || phyId == 0)
            {
                _notyf.Error("Physician Not found");
                return false;
            }

            if (model.ShiftDate == null || model.StartTime == null || model.EndTime == null)
            {
                _notyf.Error("Please enter all the required details");
                return false;
            }

            if (model.ShiftDate.Value.Date == DateTime.Today && model.StartTime < TimeOnly.FromDateTime(DateTime.Now))
            {
                _notyf.Error("Please create shifts after the current time for today's shifts.");
                return false;
            }

            if (ModelState.IsValid)
            {

                List<DateTime> totalShiftDates = new List<DateTime>();
                List<DateTime> clashingShifts = new List<DateTime>();
                bool isAnotherShiftExists = false;

                totalShiftDates.Add(model.ShiftDate.Value.Date);
                bool isExistsInit = _unitOfWork.ShiftDetailRepository.IsAnotherShiftExists(model.PhysicianId, model.ShiftDate.Value.Date, model.StartTime, model.EndTime);

                if (isExistsInit)
                {
                    isAnotherShiftExists = true;
                    clashingShifts.Add(model.ShiftDate.Value.Date);
                }

                if (model.IsRepeat != null && model.repeatDays != null)
                {
                    DateTime nextShiftDate = model.ShiftDate ?? DateTime.Now;
                    int currentDayOfWeek = (int)nextShiftDate.DayOfWeek;
                    List<int> sorted = model.repeatDays.OrderBy(_ => _).ToList();

                    int offset = GetOffSet(currentDayOfWeek, sorted);

                    for (int i = 0; i < model.RepeatCount; i++)
                    {
                        int length = sorted.Count;

                        for (int j = 0; j < length; j++)
                        {
                            int offsetIndex = (j + offset) % length;

                            nextShiftDate = GetNextWeekday(nextShiftDate, sorted[offsetIndex]);
                            totalShiftDates.Add(nextShiftDate);

                            bool isExists = _unitOfWork.ShiftDetailRepository.IsAnotherShiftExists(model.PhysicianId, nextShiftDate.Date, model.StartTime, model.EndTime);

                            if (isExists)
                            {
                                isAnotherShiftExists = true;
                                clashingShifts.Add(nextShiftDate);
                            }

                        }

                    }

                }

                if (isAnotherShiftExists)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (DateTime date in clashingShifts)
                    {
                        stringBuilder.Append(date.ToString("ddd, dd MMM yyyy"));
                    }

                    _notyf.Error("Your shifts are clashing with shifts on: \n" + stringBuilder.ToString());
                    return false;
                }


                Shift shift = new()
                {
                    Physicianid = model.PhysicianId,
                    Startdate = DateOnly.FromDateTime(model.ShiftDate.Value.Date),
                    Repeatupto = model.RepeatCount,
                    Createddate = DateTime.Now,
                    Createdby = phyAspId,
                    Isrepeat = model.IsRepeat != null,
                };

                _unitOfWork.ShiftRepository.Add(shift);
                _unitOfWork.Save();


                foreach (DateTime date in totalShiftDates)
                {

                    Shiftdetail shiftdetail = new()
                    {
                        Shiftid = shift.Shiftid,
                        Shiftdate = date,
                        Regionid = model.RegionId,
                        Starttime = (TimeOnly)model.StartTime,
                        Endtime = (TimeOnly)model.EndTime,
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

                _notyf.Success("Successfully Added shifts");
                return true;

            }

            _notyf.Error("Please enter all the required details");
            return false;
        }


        #endregion

        #region Invoicing

        [RoleAuthorize((int)AllowMenu.ProviderInvoicing)]
        public IActionResult Invoicing()
        {
            return View("Header/Invoicing");
        }

        #endregion


        #region Dashboard

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
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
            initialFilter.Status = (int)status;
            initialFilter.PageNumber = (int)page;
            initialFilter.RegionFilter = (int)region;
            initialFilter.RequestTypeFilter = (int)type;
            initialFilter.PatientSearchText = (string)search;

            PhysicianDashboardViewModel model = new PhysicianDashboardViewModel();
            model.UserName = phyName;
            model.physicians = _unitOfWork.PhysicianRepository.GetAll();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.NewReqCount = _unitOfWork.RequestRepository.Where(r => r.Physicianid == phyId && r.Status == (short)RequestStatus.Unassigned).Count();
            model.PendingReqCount = _unitOfWork.RequestRepository.Where(r => r.Physicianid == phyId && r.Status == (short)RequestStatus.Accepted).Count();
            model.ActiveReqCount = _unitOfWork.RequestRepository.Where(r => r.Physicianid == phyId && ((r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite))).Count();
            model.ConcludeReqCount = _unitOfWork.RequestRepository.Where(r => r.Physicianid == phyId && r.Status == (short)RequestStatus.Conclude).Count();
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
                PageNumber = pageNumber,
                PageSize = 5,
                Status = status,
            };

            PagedList<PhyDashboardTRow> pagedList = await _physicianService.PhysicianDashboardService.GetPhysicianRequestAsync(filter, phyId);

            PhysicianDashboardViewModel model = new PhysicianDashboardViewModel();
            model.pagedList = pagedList;
            model.DashboardStatus = status;
            model.CurrentPage = pageNumber;
            model.filterOptions = filter;

            return PartialView("Partial/PartialTable", model);
        }


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult ViewUploads(int requestId)
        {

            ViewUploadsViewModel? model = _physicianService.AdminProviderService.GetViewUploadsModel(requestId, false);

            if (model == null)
            {
                _notyf.Error("Cannot get data. Please try again later.");
                return RedirectToAction("Dashboard");
            }

            return View("AdminProvider/ViewUploads", model);
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


        [HttpPost]
        public bool SendFilesViaMail(List<int> fileIds, int requestId)
        {
            try
            {
                if (fileIds.Count < 1)
                {
                    TempData["error"] = "Please select at least one document before sending email.";
                    return false;
                }
                Requestclient? reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(requestCli => requestCli.Requestid == requestId);

                string? senderEmail = _config.GetSection("OutlookSMTP")["Sender"];
                string? senderPassword = _config.GetSection("OutlookSMTP")["Password"];

                if (reqCli == null || reqCli.Email == null || senderEmail == null)
                {
                    _notyf.Error("Could not fetch credentials");
                    return false;
                }

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
                    Subject = "Hallodoc documents attachments",
                    IsBodyHtml = true,
                    Body = "<h3>Admin has sent you documents regarding your request.</h3>",
                };

                MemoryStream memoryStream;
                foreach (int fileId in fileIds)
                {
                    Requestwisefile? file = _unitOfWork.RequestWiseFileRepository.GetFirstOrDefault(reqFile => reqFile.Requestwisefileid == fileId);

                    if (file == null)
                    {
                        continue;
                    }

                    string documentPath = Path.Combine(_environment.WebRootPath, "document", "request", requestId.ToString(), file.Filename);

                    byte[] fileBytes = System.IO.File.ReadAllBytes(documentPath);
                    memoryStream = new MemoryStream(fileBytes);
                    mailMessage.Attachments.Add(new Attachment(memoryStream, file.Filename));
                }

                mailMessage.To.Add(reqCli.Email);

                client.Send(mailMessage);

                TempData["success"] = "Email with selected documents has been successfully sent to " + reqCli.Email;
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error occured while sending documents. Please try again later.";
                return false;
            }
        }


        public async Task<IActionResult> DownloadAllFiles(int requestId)
        {
            try
            {
                // Fetch all document details for the given request:
                List<Requestwisefile> documentDetails = _unitOfWork.RequestWiseFileRepository.Where(m => m.Requestid == requestId && m.Isdeleted != true).ToList();

                if (documentDetails == null || documentDetails.Count == 0)
                {
                    return NotFound("No documents found for download");
                }

                // Create a unique zip file name
                string? zipFileName = $"Documents_{DateTime.Now:yyyyMMddHHmmss}.zip";
                string zipFilePath = Path.Combine(_environment.WebRootPath, "DownloadableZips", zipFileName);

                // Create the directory if it doesn't exist
                string? zipDirectory = Path.GetDirectoryName(zipFilePath);
                if (!Directory.Exists(zipDirectory))
                {
                    Directory.CreateDirectory(zipDirectory);
                }

                // Create a new zip archive
                using (ZipArchive zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    // Add each document to the zip archive
                    foreach (Requestwisefile document in documentDetails)
                    {
                        string documentPath = Path.Combine(_environment.WebRootPath, "document", "request", requestId.ToString(), document.Filename);
                        zipArchive.CreateEntryFromFile(documentPath, document.Filename);
                    }
                }

                // Return the zip file for download
                byte[] zipFileBytes = await System.IO.File.ReadAllBytesAsync(zipFilePath);
                return File(zipFileBytes, "application/zip", zipFileName);
            }
            catch
            {
                return BadRequest("Error downloading files");
            }
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
            try
            {

                EncounterFormViewModel? model = _physicianService.AdminProviderService.GetEncounterFormModel(requestId ?? 0, true);

                if (model == null)
                {
                    _notyf.Error("Coundn't fetch encounter data. Please try again later");
                    return RedirectToAction("Dashboard");
                }

                return new ViewAsPdf("Partial/PdfPartial", model)
                {
                    FileName = "FinalizedEncounterForm.pdf"
                };
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
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

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
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

        [HttpPost]
        public IActionResult ConcludeCasePhysician(int requestId, string phyNotes)
        {

            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {

                Encounterform? encounterform = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(form => form.Requestid == requestId);
                if (encounterform == null || !encounterform.Isfinalize)
                {
                    TempData["error"] = "Please finalize encounter";
                    _notyf.Error("Please finalize encounter form before concluding the case.");
                    return RedirectToAction("ConcludeCare", new { requestId = requestId });
                }

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

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult EncounterForm(int requestId)
        {
            try
            {

                EncounterFormViewModel? model = _physicianService.AdminProviderService.GetEncounterFormModel(requestId, false);

                if (model == null)
                {
                    _notyf.Error("Coundn't fetch data");
                    return RedirectToAction("Dashboard");
                }

                return View("AdminProvider/EncounterForm", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EncounterForm(EncounterFormViewModel model)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            try
            {

                if (ModelState.IsValid)
                {
                    ServiceResponse response = _physicianService.AdminProviderService.SubmitEncounterForm(model, false, phyId);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success("Encounter form updated successfully.");
                        return RedirectToAction("EncounterForm", new { requestId = model.RequestId });
                    }

                    _notyf.Error(response.Message);
                    return View("AdminProvider/EncounterForm", model);

                }

                _notyf.Error("Please ensure all details are valid.");
                return View("AdminProvider/EncounterForm", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }

        }


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        [HttpPost]
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


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult CreateRequest()
        {
            IEnumerable<Region> regions = _unitOfWork.RegionRepository.GetAll();
            AdminCreateRequestViewModel model = new AdminCreateRequestViewModel();
            model.regions = regions;
            model.IsAdmin = false;
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

                    ServiceResponse response = _physicianService.AdminProviderService.SubmitCreateRequest(model, phyAspId, createLink ?? "", false);

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


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult Orders(int requestId)
        {

            SendOrderViewModel model = new SendOrderViewModel();
            model.professionalTypeList = _unitOfWork.HealthProfessionalTypeRepo.GetAll();
            model.RequestId = requestId;
            model.IsAdmin = false;

            return View("AdminProvider/Orders", model);
        }

        [HttpPost]
        public IActionResult Orders(SendOrderViewModel orderViewModel)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

            try
            {
                if (ModelState.IsValid)
                {

                    ServiceResponse response = _physicianService.AdminProviderService.SubmitOrderDetails(orderViewModel, phyAspId);

                    if (response.StatusCode == ResponseCode.Success)
                    {

                        _notyf.Success("Order Details saved successfully.");

                        return RedirectToAction("Dashboard");
                    }

                    _notyf.Error(response.Message);
                    return View("AdminProvider/Orders", orderViewModel);
                }

                _notyf.Error("Please enter all values correctly.");
                return View("AdminProvider/Orders", orderViewModel);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("AdminProvider/Orders", orderViewModel);
            }

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
            int count = _unitOfWork.RequestRepository.Where(req => req.Createddate > todayStart).Count();

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
