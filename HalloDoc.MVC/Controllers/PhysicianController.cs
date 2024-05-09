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
using Business_Layer.Services.PhysicianServices.Interface;
using System.IO.Compression;
using Data_Layer.CustomModels.Filter;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Data_Layer.DataContext;
using Humanizer.DateTimeHumanizeStrategy;

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

        public PhysicianController(IUnitOfWork unit, ApplicationDbContext context, IPhysicianService physicianService, IAdminDashboardService dashboardRepository, IConfiguration config, IPhysicianDashboardService physicalDashboardService, INotyfService notyf, IWebHostEnvironment webHostEnvironment, IEmailService emailService)
        {
            _unitOfWork = unit;
            _config = config;
            _notyf = notyf;
            _environment = webHostEnvironment;
            _emailService = emailService;
            _physicianService = physicianService;
        }

        #region Header

        public int GetPhysicianId()
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            return phyId;
        }

        public string? GetPhysicianName()
        {
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            return phyName;
        }

        public string? GetPhysicianAspUserId()
        {
            string? phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;
            return phyAspId;
        }

        public IActionResult Logout()
        {

            Response.Cookies.Delete("hallodoc");
            _notyf.Success("Logout Successfull");

            return Redirect("/Guest/Login");
        }

        #endregion

        #region Profile


        [RoleAuthorize((int)AllowMenu.ProviderProfile)]
        public IActionResult Profile()
        {
            try
            {
                int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
                string? phyAspId = GetPhysicianAspUserId();

                Physician? physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Aspnetuserid == phyAspId);

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

                return View("Profile/Profile", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);

                return false;
            }

        }

        [HttpPost]
        public bool SendMessageToAdmin(string message)
        {
            try
            {

                int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
                string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
                _emailService.SendMailToAdminForEditProfile(message, phyId, phyName);

                _notyf.Success("Mail successfully send to admin");

                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        #endregion

        #region Schedule

        [RoleAuthorize((int)AllowMenu.ProviderSchedule)]
        public IActionResult ShowDayShiftsModal(DateTime jsDate)
        {
            try
            {

                DateTime shiftDate = jsDate.ToLocalTime().Date;
                DayShiftModel model = new DayShiftModel()
                {
                    ShiftDate = shiftDate,
                    shiftdetails = _unitOfWork.ShiftDetailRepository.Where(shift => shift.Shiftdate == shiftDate),
                };

                return PartialView("Schedule/DayShiftModal", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Error");
            }
        }

        [RoleAuthorize((int)AllowMenu.ProviderSchedule)]
        public IActionResult Schedule()
        {
            try
            {
                SchedulingViewModel model = new SchedulingViewModel();
                model.regions = _unitOfWork.RegionRepository.GetAll();

                return View("Schedule/MySchedule", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [RoleAuthorize((int)AllowMenu.ProviderSchedule)]
        public IActionResult LoadMonthSchedule(int shiftMonth, int shiftYear)
        {
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return PartialView("Schedule/Partial/_MonthViewPartial");
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

            if (index < 0) index = 0;
            return index;
        }

        public IActionResult ViewShiftModal(int shiftDetailId)
        {
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Error");
            }

        }

        [HttpGet]
        public IActionResult AddShiftModal()
        {
            try
            {

                int phyId = GetPhysicianId();

                IEnumerable<int> phyRegions = _unitOfWork.PhysicianRegionRepo.Where(phy => phy.Physicianid == phyId).Select(_ => _.Regionid);
                AddShiftModel model = new AddShiftModel();
                model.regions = _unitOfWork.RegionRepository.Where(reg => phyRegions.Contains(reg.Regionid));
                model.PhysicianId = phyId;
                return PartialView("Schedule/AddShiftModal", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Error");
            }
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
                    _notyf.Error("Cannot Find Shift");
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
                _notyf.Error(e.Message);
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
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }


        #endregion

        #region Invoicing

        [RoleAuthorize((int)AllowMenu.ProviderInvoicing)]
        public IActionResult Invoicing()
        {
            return View("Invoicing/Invoicing");
        }

        public IActionResult LoadInvoicingPartialTable(DateTime startDateISO)
        {
            try
            {
                int phyId = GetPhysicianId();

                DateTime startDate = startDateISO.ToLocalTime();
                DateTime endDate = startDate;
                DateTime firstDayOfMonth = new DateTime(startDate.Year, startDate.Month, 1);

                if (startDate.Day < 15)
                {
                    startDate = firstDayOfMonth;
                    endDate = new DateTime(startDate.Year, startDate.Month, 14);
                }
                else
                {
                    startDate = new DateTime(startDate.Year, startDate.Month, 15);
                    endDate = firstDayOfMonth.AddMonths(1).AddTicks(-1);
                }

                Timesheet? timesheet = _unitOfWork.TimeSheetRepository.GetFirstOrDefault(sheet => sheet.PhysicianId == phyId
                && sheet.StartDate == DateOnly.FromDateTime(startDate) && sheet.EndDate == DateOnly.FromDateTime(endDate));

                if (timesheet == null)
                {
                    return PartialView("Invoicing/Partial/_InvoicingTimeSheetTable");
                }

                InvoicingTimeSheetViewModel model = new InvoicingTimeSheetViewModel();

                model.StartDate = DateOnly.FromDateTime(startDate);
                model.EndDate = DateOnly.FromDateTime(endDate);
                model.timeSheetRecords = (from record in _unitOfWork.TimeSheetDetailRepo.GetAll()
                                          where record.TimesheetId == timesheet.TimesheetId
                                          select new InvoicingTimeSheetTRow
                                          {
                                              TimeSheetDetailId = record.TimesheetDetailId,
                                              ShiftDate = record.TimesheetDate,
                                              ShiftCount = -1,
                                              HouseCall = record.NumberOfHouseCall ?? 0,
                                              HouseCallNightWeekendCount = -1,
                                              PhoneConsults = record.NumberOfPhoneCall ?? 0,
                                              PhoneConsultsNightWeekendCount = -1,
                                              NightShiftsWeekendCount = -1,
                                              BatchTesting = -1,
                                          }).ToList();

                List<ReceiptRecord> records = new List<ReceiptRecord>();

                foreach (var sheetDetail in model.timeSheetRecords)
                {
                    TimesheetDetailReimbursement? receipt = _unitOfWork.TimeSheetDetailReimbursementRepo.GetFirstOrDefault(record => record.TimesheetDetailId == sheetDetail.TimeSheetDetailId);

                    if (receipt != null)
                    {
                        ReceiptRecord record = new ReceiptRecord
                        {
                            DateOfRecord = sheetDetail.ShiftDate,
                            ItemName = receipt.ItemName,
                            Amount = receipt.Amount,
                            BillReceiptName = receipt.Bill,
                            BillReceiptFilePath = $"/document/timesheet/physician{phyId}/{timesheet.TimesheetId}/{receipt.TimesheetDetailReimbursementId}.pdf",
                        };

                        records.Add(record);
                    }
                }

                model.receiptRecords = records;
                return PartialView("Invoicing/Partial/_InvoicingTimeSheetTable", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return PartialView("Invoicing/Partial/_InvoicingTimeSheetTable");
            }
        }


        [RoleAuthorize((int)AllowMenu.ProviderInvoicing)]
        public IActionResult TimeSheetForm(DateTime startDateISO)
        {
            try
            {
                int phyId = GetPhysicianId();

                DateOnly startDate = DateOnly.FromDateTime(startDateISO.ToLocalTime());
                DateOnly endDate = startDate;

                DateOnly firstDayOfMonth = new DateOnly(startDate.Year, startDate.Month, 1);

                if (startDate.Day < 15)
                {
                    startDate = firstDayOfMonth;
                    endDate = new DateOnly(startDate.Year, startDate.Month, 14);
                }
                else
                {
                    startDate = new DateOnly(startDate.Year, startDate.Month, 15);
                    endDate = firstDayOfMonth.AddMonths(1).AddDays(-1);
                }

                Timesheet? oldTimeSheet = _unitOfWork.TimeSheetRepository.GetFirstOrDefault(sheet =>
                sheet.PhysicianId == phyId
                && sheet.StartDate == startDate
                && sheet.EndDate == endDate
                );



                if (oldTimeSheet == null)
                {

                    DateOnly loopDate = startDate;

                    List<TimeSheetDayRecord> records = new List<TimeSheetDayRecord>();
                    while (loopDate <= endDate)
                    {
                        TimeSheetDayRecord record = new TimeSheetDayRecord()
                        {
                            DateOfRecord = loopDate,
                        };

                        records.Add(record);
                        loopDate = loopDate.AddDays(1);
                    }


                    DateOnly receiptDate = startDate;

                    List<ReceiptRecord> receiptRecords = new List<ReceiptRecord>();
                    while (receiptDate <= endDate)
                    {
                        ReceiptRecord record = new ReceiptRecord()
                        {
                            DateOfRecord = receiptDate,
                        };

                        receiptRecords.Add(record);
                        receiptDate = receiptDate.AddDays(1);
                    }


                    TimeSheetFormViewModel model = new TimeSheetFormViewModel()
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        timeSheetDayRecords = records,
                        timeSheetReceiptRecords = receiptRecords,
                    };

                    return View("Invoicing/TimeSheetForm", model);
                }
                else
                {
                    if (oldTimeSheet?.IsFinalize == true)
                    {
                        _notyf.Error("Timesheet is already finalized.");
                        return RedirectToAction("Invoicing");
                    }

                    TimeSheetFormViewModel? model = GetExistingTimeSheetViewModel(oldTimeSheet?.TimesheetId ?? 0, startDate, endDate);

                    if (model == null)
                    {
                        _notyf.Error("Could not fetch data");
                        return RedirectToAction("Invoicing");
                    }

                    return View("Invoicing/TimeSheetForm", model);
                }

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Invoicing");
            }
        }

        public TimeSheetFormViewModel? GetExistingTimeSheetViewModel(int timeSheetId, DateOnly startDate, DateOnly endDate)
        {

            try
            {
                int phyId = GetPhysicianId();

                IEnumerable<TimeSheetDayRecord> records = (from record in _unitOfWork.TimeSheetDetailRepo.GetAll()
                                                           where record.TimesheetId == timeSheetId
                                                           select new TimeSheetDayRecord
                                                           {
                                                               TimeSheetDetailId = record.TimesheetDetailId,
                                                               DateOfRecord = record.TimesheetDate,
                                                               IsHoliday = record.IsWeekend ?? false,
                                                               NoOfHouseCall = record.NumberOfHouseCall ?? 0,
                                                               NoOfPhoneConsult = record.NumberOfPhoneCall ?? 0,
                                                               OnCallHours = -1,
                                                               TotalHours = record.TotalHours ?? 0,
                                                           }).OrderBy(_ => _.DateOfRecord);

                DateOnly receiptDate = startDate;

                List<ReceiptRecord> receiptRecords = new List<ReceiptRecord>();
                while (receiptDate <= endDate)
                {
                    int? timeSheetDetailId = records.FirstOrDefault(record => record.DateOfRecord == receiptDate)?.TimeSheetDetailId;

                    TimesheetDetailReimbursement? reimbursement = _unitOfWork.TimeSheetDetailReimbursementRepo.GetFirstOrDefault(receipt=> receipt.TimesheetDetailId == timeSheetDetailId);
                    
                    ReceiptRecord record = new ReceiptRecord()
                    {
                        TimeSheetDetailId = timeSheetDetailId ?? 0,
                        DateOfRecord = receiptDate,
                    };

                    if (reimbursement != null)
                    {
                        record = new ReceiptRecord()
                        {
                            TimeSheetDetailId = timeSheetDetailId ?? 0,
                            DateOfRecord = receiptDate,
                            ItemName = reimbursement.ItemName,
                            Amount = reimbursement.Amount,
                            BillReceiptName = reimbursement.Bill,
                            BillReceiptFilePath = $"/document/timesheet/physician{phyId}/{timeSheetId}/{reimbursement.TimesheetDetailReimbursementId}.pdf",
                        };
                    }

                    receiptRecords.Add(record);
                    receiptDate = receiptDate.AddDays(1);
                }

                TimeSheetFormViewModel model = new TimeSheetFormViewModel()
                {
                    TimesheetId = timeSheetId,
                    StartDate = startDate,
                    EndDate = endDate,
                    timeSheetDayRecords = records.ToList(),
                    timeSheetReceiptRecords = receiptRecords,
                };

                return model;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return null;
            }
        }

        public bool SubmitNewTimesheet(TimeSheetFormViewModel model)
        {
            int phyId = GetPhysicianId();
            string? phyAspId = GetPhysicianAspUserId();

            try
            {

                Timesheet newTimeSheet = new Timesheet()
                {
                    PhysicianId = phyId,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsFinalize = false,
                    IsApproved = false,
                    CreatedBy = phyAspId ?? "",
                    CreatedDate = DateTime.Now,
                };

                _unitOfWork.TimeSheetRepository.Add(newTimeSheet);
                _unitOfWork.Save();

                if (model.IsReceiptsAdded)
                {
                    if (model.timeSheetDayRecords != null && model.timeSheetDayRecords.Any())
                    {
                        foreach (TimeSheetDayRecord record in model.timeSheetDayRecords)
                        {
                            TimesheetDetail sheetDetail = new TimesheetDetail()
                            {
                                TimesheetId = newTimeSheet.TimesheetId,
                                TimesheetDate = record.DateOfRecord,
                                TotalHours = record.TotalHours,
                                IsWeekend = record.IsHoliday,
                                NumberOfHouseCall = record.NoOfHouseCall,
                                NumberOfPhoneCall = record.NoOfPhoneConsult,
                                CreatedBy = phyAspId ?? "",
                                CreatedDate = DateTime.Now,
                            };

                            _unitOfWork.TimeSheetDetailRepo.Add(sheetDetail);
                            _unitOfWork.Save();

                            ReceiptRecord? receipt = model.timeSheetReceiptRecords?.FirstOrDefault(receipt => receipt.DateOfRecord == record.DateOfRecord);


                            if (receipt == null || receipt.ItemName == null || receipt.Amount == 0 || receipt.BillReceipt == null)
                            {
                                continue;
                            }


                            TimesheetDetailReimbursement recordDetail = new TimesheetDetailReimbursement()
                            {
                                TimesheetDetailId = newTimeSheet.TimesheetId,
                                ItemName = receipt.ItemName,
                                Amount = receipt.Amount,
                                Bill = receipt.BillReceipt.FileName,
                                CreatedBy = phyAspId ?? "",
                                CreatedDate = DateTime.Now,
                            };

                            _unitOfWork.TimeSheetDetailReimbursementRepo.Add(recordDetail);
                            _unitOfWork.Save();

                            FileHelper.InsertFileForTimeSheetReceipt(receipt.BillReceipt, _environment.WebRootPath, phyId, newTimeSheet.TimesheetId, recordDetail.TimesheetDetailReimbursementId);

                        }

                        _notyf.Success("Time Sheet Added Successfully");

                    }
                }
                else
                {
                    if (model.timeSheetDayRecords != null && model.timeSheetDayRecords.Any())
                    {
                        foreach (TimeSheetDayRecord record in model.timeSheetDayRecords)
                        {
                            TimesheetDetail sheetDetail = new TimesheetDetail()
                            {
                                TimesheetId = newTimeSheet.TimesheetId,
                                TimesheetDate = record.DateOfRecord,
                                TotalHours = record.TotalHours,
                                IsWeekend = record.IsHoliday,
                                NumberOfHouseCall = record.NoOfHouseCall,
                                NumberOfPhoneCall = record.NoOfPhoneConsult,
                                CreatedBy = phyAspId ?? "",
                                CreatedDate = DateTime.Now,
                            };

                            _unitOfWork.TimeSheetDetailRepo.Add(sheetDetail);
                        }

                        _unitOfWork.Save();

                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        public bool UpdateExistingTimesheet(TimeSheetFormViewModel model, int timeSheetId)
        {
            int phyId = GetPhysicianId();
            string? phyAspId = GetPhysicianAspUserId();
            try
            {

                DateOnly loopDate = model.StartDate;

                while (loopDate <= model.EndDate)
                {

                    TimesheetDetail? sheetDetail = _unitOfWork.TimeSheetDetailRepo.GetFirstOrDefault(sheet => sheet.TimesheetId == timeSheetId
                    && sheet.TimesheetDate == loopDate);
                    TimeSheetDayRecord? inputRecord = model.timeSheetDayRecords?.FirstOrDefault(record => record.DateOfRecord == loopDate);

                    if (sheetDetail != null && inputRecord != null)
                    {
                        sheetDetail.TotalHours = inputRecord.TotalHours;
                        sheetDetail.IsWeekend = inputRecord.IsHoliday;
                        sheetDetail.NumberOfHouseCall = inputRecord.NoOfHouseCall;
                        sheetDetail.NumberOfPhoneCall = inputRecord.NoOfPhoneConsult;

                        _unitOfWork.TimeSheetDetailRepo.Update(sheetDetail);
                    }

                    loopDate = loopDate.AddDays(1);

                }

                if (model.IsReceiptsAdded)
                {

                    loopDate = model.StartDate;

                    while (loopDate <= model.EndDate)
                    {

                        ReceiptRecord? inputRecord = model.timeSheetReceiptRecords?.FirstOrDefault(record => record.DateOfRecord == loopDate);

                        if (inputRecord == null || inputRecord.ItemName == null || inputRecord.Amount <= 0 || inputRecord.BillReceipt == null)
                        {
                            loopDate = loopDate.AddDays(1);
                            continue;
                        }

                        TimesheetDetailReimbursement? reimbursementDetail = _unitOfWork.TimeSheetDetailReimbursementRepo.GetFirstOrDefault(sheet => sheet.TimesheetDetailReimbursementId == inputRecord.RecordId);

                        if (reimbursementDetail != null)
                        {
                            reimbursementDetail.ItemName = inputRecord.ItemName;
                            reimbursementDetail.Amount = inputRecord.Amount;
                            reimbursementDetail.Bill = inputRecord.BillReceipt.FileName;
                            reimbursementDetail.ModifiedBy = phyAspId;
                            reimbursementDetail.ModifiedDate = DateTime.Now;
                            _unitOfWork.TimeSheetDetailReimbursementRepo.Update(reimbursementDetail);
                            _unitOfWork.Save();
                        }
                        else
                        {
                            TimeSheetDayRecord? timeSheetDetailRecord = model.timeSheetDayRecords?.FirstOrDefault(record => record.DateOfRecord == loopDate);

                            reimbursementDetail = new TimesheetDetailReimbursement()
                            {
                                TimesheetDetailId = timeSheetDetailRecord.TimeSheetDetailId,
                                ItemName = inputRecord.ItemName,
                                Amount = inputRecord.Amount,
                                Bill = inputRecord.BillReceipt.FileName,
                                CreatedBy = phyAspId,
                                CreatedDate = DateTime.Now,
                            };

                            _unitOfWork.TimeSheetDetailReimbursementRepo.Add(reimbursementDetail);
                            _unitOfWork.Save();
                        }

                        FileHelper.InsertFileForTimeSheetReceipt(inputRecord.BillReceipt, _environment.WebRootPath,
                            phyId, timeSheetId, reimbursementDetail.TimesheetDetailReimbursementId);
                        loopDate = loopDate.AddDays(1);

                    }
                }

                _unitOfWork.Save();
                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }


        [HttpPost]
        public IActionResult TimeSheetForm(TimeSheetFormViewModel model)
        {
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? phyAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;
            try
            {

                Timesheet? oldTimeSheet = _unitOfWork.TimeSheetRepository.GetFirstOrDefault(sheet =>
                sheet.PhysicianId == phyId
                && sheet.StartDate == model.StartDate
                && sheet.EndDate == model.EndDate
                );

                if (oldTimeSheet == null)
                {
                    bool isSuccess = SubmitNewTimesheet(model);
                    if (!isSuccess)
                    {
                        return View("Invoicing/TimeSheetForm", model);
                    }
                    _notyf.Success("Time Sheet Added Successfully");

                }
                else
                {

                    bool isSuccess = UpdateExistingTimesheet(model, oldTimeSheet.TimesheetId);
                    if (!isSuccess)
                    {
                        return View("Invoicing/TimeSheetForm", model);
                    }
                    _notyf.Success("Time Sheet Updated Successfully");
                }

                return RedirectToAction("Invoicing");
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Invoicing/TimeSheetForm", model);
            }

        }

        [HttpPost]
        public IActionResult FinalizeTimeSheet(TimeSheetFormViewModel model)
        {
            Timesheet? timesheet = _unitOfWork.TimeSheetRepository.GetFirstOrDefault(sheet => sheet.TimesheetId == model.TimesheetId);
            
            if(timesheet == null)
            {
                _notyf.Error("Timesheet not found");
                return RedirectToAction("Invoicing");
            }

            TimeSheetForm(model);

            timesheet.IsFinalize = true;
            _unitOfWork.TimeSheetRepository.Update(timesheet);
            _unitOfWork.Save();

            _notyf.Success("Timesheet successfully finalized");
            return RedirectToAction("Invoicing");
        }

        #endregion

        #region Dashboard

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult Dashboard()
        {
            try
            {

                string? phyName = GetPhysicianName();
                int phyId = GetPhysicianId();

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

                return View("Dashboard/Dashboard", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Login", "Guest");
            }

        }

        public async Task<string> GetAddressFromLatLng(double latitude, double longtitude)
        {
            try
            {

                using (HttpClient client = new HttpClient())
                {
                    string? apiKey = _config.GetSection("Geocoding")["ApiKey"];
                    string baseUrl = $"https://geocode.maps.co/reverse?lat={latitude}&lon={longtitude}&api_key={apiKey}";
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
                _notyf.Error($"Error updating location :{ex.Message}");
                return false;
            }

        }


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult ViewNotes(int requestId)
        {
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
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
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }

        }

        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        [HttpPost]
        public async Task<ActionResult> PartialTable(int status, int page, int typeFilter, string searchFilter, int regionFilter)
        {
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return PartialView("Partial/PartialTable");
            }
        }


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult ViewUploads(int requestId)
        {
            try
            {

                ViewUploadsViewModel? model = _physicianService.AdminProviderService.GetViewUploadsModel(requestId, false);

                if (model == null)
                {
                    _notyf.Error("Cannot get data. Please try again later.");
                    return RedirectToAction("Dashboard");
                }

                return View("AdminProvider/ViewUploads", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult ViewUploads(ViewUploadsViewModel uploadsVM)
        {
            try
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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("AdminProvider/ViewUploads", uploadsVM);
            }
        }


        [HttpPost]
        public bool SendFilesViaMail(List<int> fileIds, int requestId)
        {
            try
            {
                if (fileIds.Count < 1)
                {
                    _notyf.Error("Please select at least one document before sending email.");
                    return false;
                }
                Requestclient? reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(requestCli => requestCli.Requestid == requestId);

                if (reqCli == null || reqCli.Email == null)
                {
                    _notyf.Error("Could not fetch user data");
                    return false;
                }

                MemoryStream memoryStream;
                List<Attachment> mailAttachments = new List<Attachment>();
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

                    Attachment attachment = new Attachment(memoryStream, file.Filename);
                    mailAttachments.Add(attachment);
                }

                _emailService.SendMailWithFilesAttached(reqCli.Email, mailAttachments);

                _notyf.Success("Email with selected documents has been successfully sent to " + reqCli.Email);
                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error("Error occured while sending documents. Please try again later.");
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

                _notyf.Success("Files deleted Succesfully.");
                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error("Error occured while deleting files.");
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

                _notyf.Success("File deleted Succesfully.");
                return true;
            }
            catch (Exception e)
            {
                _notyf.Error("Error occured while deleting file.");
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
                _emailService.SendMailForPatientAgreement(model.RequestId, model.PatientEmail);

                _notyf.Success("Agreement Sent Successfully.");
                return Redirect("/Admin/Dashboard");

            }
            catch (Exception ex)
            {
                _notyf.Error("An error occurred while sending agreement.");
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
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

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
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

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
            string? phyName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {

                Encounterform? encounterform = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(form => form.Requestid == requestId);
                if (encounterform == null || !encounterform.Isfinalize)
                {
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

                _notyf.Success("Successfully Concluded Request.");

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
            try
            {

                IEnumerable<Region> regions = _unitOfWork.RegionRepository.GetAll();
                AdminCreateRequestViewModel model = new AdminCreateRequestViewModel();
                model.regions = regions;
                model.IsAdmin = false;
                return View("AdminProvider/CreateRequest", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
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

                _notyf.Success("Email has been successfully sent to " + email + " for create account link.");
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
            }
        }


        [RoleAuthorize((int)AllowMenu.ProviderDashboard)]
        public IActionResult Orders(int requestId)
        {
            try
            {

                SendOrderViewModel model = new SendOrderViewModel();
                model.professionalTypeList = _unitOfWork.HealthProfessionalTypeRepo.GetAll();
                model.RequestId = requestId;
                model.IsAdmin = false;

                return View("AdminProvider/Orders", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
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
            int phyId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            if (ModelState.IsValid)
            {
                try
                {

                    string patientName = model.FirstName + " " + model.LastName;
                    _emailService.SendMailForSubmitRequest(patientName, model.Email, false, phyId);

                    Smslog smsLog = new Smslog()
                    {
                        Smstemplate = "1",
                        Mobilenumber = model.Phone,
                        Roleid = (int)AccountType.Patient,
                        Physicianid = phyId,
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
                    _notyf.Error("Error occurred : " + e.Message);
                    return Redirect("/Physician/Dashboard");
                }
            }

            _notyf.Error("Please Fill all details for sending link.");
            return Redirect("/Physician/Dashboard");
        }

        #endregion

    }
}
