using Business_Layer.Helpers;
using Business_Layer.Interface;
using Business_Layer.Interface.AdminInterface;
using Business_Layer.Interface.Services;
using Business_Layer.Utilities;
using ClosedXML.Excel;
using CsvHelper;
using Data_Layer.CustomModels;
using Data_Layer.CustomModels.TableRow;
using Data_Layer.CustomModels.TableRow.Admin;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Data_Layer.ViewModels.Admin;
using HalloDoc.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json.Nodes;

namespace HalloDoc.MVC.Controllers
{

    [CustomAuthorize((int)AllowRole.Admin)]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IUtilityService _utilityService;
        private readonly ApplicationDbContext _context;

        public AdminController(IUnitOfWork unitOfWork, IDashboardRepository dashboard, IWebHostEnvironment environment, IConfiguration config, ApplicationDbContext context, IEmailService emailService, IUtilityService utilityService)
        {
            _unitOfWork = unitOfWork;
            _dashboardRepository = dashboard;
            _environment = environment;
            _config = config;
            _emailService = emailService;
            _context = context;
            _utilityService = utilityService;
        }


        #region Header

        public IActionResult Profile()
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);

            if (admin == null)
            {
                TempData["error"] = "Admin not found , please login!";
                return RedirectToAction("Index");
            }

            string state = _unitOfWork.RegionRepository.GetFirstOrDefault(r => r.Regionid == admin.Regionid).Name;

            IEnumerable<Region> regions = _unitOfWork.RegionRepository.GetAll();
            IEnumerable<int> adminRegions = _unitOfWork.AdminRegionRepo.Where(region => region.Adminid == adminId).ToList().Select(x => (int)x.Regionid);
            IEnumerable<City> adminMailCityList = _context.Cities.Where(city => city.Regionid == admin.Regionid);
            int cityId = _context.Cities.FirstOrDefault(city=> city.Name == admin.City)?.Id ?? 0;

            AdminProfileViewModel model = new()
            {
                Username = admin.Firstname + " " + admin.Lastname,
                StatusId = admin.Status,
                RoleId = admin.Roleid,
                FirstName = admin.Firstname,
                Email = admin.Email,
                ConfirmEmail = admin.Email,
                PhoneNumber = admin.Mobile,
                AltPhoneNumber = admin.Altphone,
                LastName = admin.Lastname,
                regions = regions,
                adminMailCities = adminMailCityList,
                Address1 = admin.Address1,
                Address2 = admin.Address2,
                City = admin.City,
                State = state,
                Zip = admin.Zip,
                RegionId = admin.Regionid ?? 0,
                selectedRegions = adminRegions,
                CityId = cityId,
            };

            return View("Header/Profile", model);

        }

        public IActionResult Partners()
        {
            return View("Header/Partners");
        }

        public IActionResult ProviderLocation()
        {
            IEnumerable<PhyLocationRow> list = (from pl in _context.Physicianlocations
                                                select new PhyLocationRow
                                                {
                                                    PhysicianName = pl.Physicianname,
                                                    Latitude = pl.Latitude ?? 0,
                                                    Longitude = pl.Longitude ?? 0,
                                                });
            string apiKey = _config.GetSection("TomTom")["ApiKey"];
            ProviderLocationViewModel model = new ProviderLocationViewModel()
            {
                locationList = list,
                ApiKey = apiKey,
            };
            return View("Header/ProviderLocation", model);
        }

        public IActionResult Logout()
        {

            Response.Cookies.Delete("hallodoc");


            TempData["success"] = "Logout Successfull";

            return Redirect("/Guest/Login");
        }

        [HttpPost]
        public async Task<ActionResult> PartialTable(int status, int page, int typeFilter, string searchFilter, int regionFilter)
        {

            HttpContext.Session.SetInt32("currentStatus", status);
            HttpContext.Session.SetInt32("currentPage", page);
            HttpContext.Session.SetInt32("currentTypeFilter", typeFilter);
            HttpContext.Session.SetInt32("currentRegionFilter", regionFilter);
            HttpContext.Session.SetString("currentSearchFilter", searchFilter ?? "");

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

            PagedList<AdminRequest> pagedList = await _dashboardRepository.GetAdminRequestsAsync(filter);

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.pagedList = pagedList;
            model.DashboardStatus = status;
            model.CurrentPage = pageNumber;
            model.filterOptions = filter;

            return PartialView("Partial/PartialTable", model);
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult Dashboard()
        {

            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            if (adminName == null)
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


            AdminDashboardViewModel model = new AdminDashboardViewModel();
            if (adminName != null)
            {
                model.UserName = adminName;
            }

            model.physicians = _unitOfWork.PhysicianRepository.GetAll();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.NewReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unassigned);
            model.PendingReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Accepted);
            model.ActiveReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite));
            model.ConcludeReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Conclude);
            model.ToCloseReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed));
            model.UnpaidReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unpaid);
            model.casetags = _unitOfWork.CaseTagRepository.GetAll();
            model.filterOptions = initialFilter;

            return View("Header/Dashboard", model);

        }

        #endregion

        #region Dashboard

        #region Modals

        [HttpGet]
        public IActionResult CancelCaseModal(int requestId, string patientName)
        {
            CancelCaseModel model = new CancelCaseModel()
            {
                RequestId = requestId,
                PatientName = patientName,
                casetags = _unitOfWork.CaseTagRepository.GetAll(),
            };
            return PartialView("Modals/CancelCaseModal", model);

        }


        [HttpPost]
        public IActionResult CancelCaseModal(CancelCaseModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value;

            try
            {

                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == model.RequestId);
                req.Status = (short)RequestStatus.Cancelled;
                req.Casetag = _unitOfWork.CaseTagRepository.GetFirstOrDefault(tag => tag.Casetagid == model.ReasonId).Name;
                req.Modifieddate = currentTime;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                string logNotes = adminName + " cancelled this request on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + model.ReasonId;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = model.RequestId,
                    Status = (short)RequestStatus.Cancelled,
                    Adminid = adminId,
                    Notes = logNotes,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Cancelled";
                return Redirect("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while cancelling request.";
                return Redirect("/Admin/Dashboard");
            }

        }

        [HttpGet]
        public IActionResult AssignCaseModal(int requestId)
        {
            AssignCaseModel model = new AssignCaseModel()
            {
                RequestId = requestId,
                regions = _unitOfWork.RegionRepository.GetAll(),
            };

            return PartialView("Modals/AssignCaseModal", model);

        }

        [HttpPost]
        public IActionResult AssignCaseModal(AssignCaseModel model)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            if (model.RequestId == null || model.RequestId <= 0 || model.PhysicianId == null || model.PhysicianId <= 0)
            {
                TempData["error"] = "Error occured while assigning request.";
                return Redirect("/Admin/Dashboard");
            }

            try
            {
                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == model.RequestId);
                req.Status = (short)RequestStatus.Accepted;
                req.Modifieddate = currentTime;
                req.Physicianid = model.PhysicianId;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Physician phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == model.PhysicianId);
                string logNotes = adminName + " assigned to " + phy.Firstname + " " + phy.Lastname + " on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + model.Notes;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = model.RequestId,
                    Status = (short)RequestStatus.Accepted,
                    Adminid = adminId,
                    Notes = logNotes,
                    Transtophysicianid = req.Physicianid,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Assigned.";
                return Redirect("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while assigning request.";
                return Redirect("/Admin/Dashboard");
            }

        }

        [HttpGet]
        public IActionResult TransferCaseModal(int requestId)
        {
            AssignCaseModel model = new AssignCaseModel()
            {
                RequestId = requestId,
                regions = _unitOfWork.RegionRepository.GetAll(),
            };

            return PartialView("Modals/TransferCaseModal", model);

        }


        [HttpPost]
        public IActionResult TransferCaseModal(AssignCaseModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            if (model.RequestId == null || model.RequestId <= 0 || model.PhysicianId == null || model.PhysicianId <= 0)
            {
                TempData["error"] = "Error occured while transfering request.";
                return Redirect("/Admin/Dashboard");
            }
            try
            {
                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == model.RequestId);
                req.Status = (short)RequestStatus.Accepted;
                req.Modifieddate = currentTime;
                req.Physicianid = model.PhysicianId;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Physician phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == model.PhysicianId);

                string logNotes = adminName + " tranferred to " + phy.Firstname + " " + phy.Lastname + " on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + model.Notes;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = model.RequestId,
                    Status = (short)RequestStatus.Accepted,
                    Adminid = adminId,
                    Notes = logNotes,
                    Transtophysicianid = model.PhysicianId,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Transferred.";
                return Redirect("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while transfering request.";
                return Redirect("/Admin/Dashboard");
            }

        }


        [HttpGet]
        public IActionResult BlockCaseModal(int requestId, string patientName)
        {
            BlockCaseModel model = new BlockCaseModel()
            {
                RequestId = requestId,
                PatientName = patientName,
            };
            return PartialView("Modals/BlockCaseModal", model);
        }

        [HttpPost]
        public IActionResult BlockCaseModal(BlockCaseModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value;

            try
            {
                DateTime currentTime = DateTime.Now;

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == model.RequestId);
                req.Status = (short)RequestStatus.Block;
                req.Modifieddate = currentTime;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                string logNotes = adminName + " blocked this request on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + model.Reason;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = model.RequestId,
                    Status = (short)RequestStatus.Block,
                    Adminid = adminId,
                    Notes = logNotes,
                    Physicianid = req.Physicianid,
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                Requestclient reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == model.RequestId);

                Blockrequest blockrequest = new Blockrequest()
                {
                    Phonenumber = reqCli.Phonenumber,
                    Email = reqCli.Email,
                    Reason = model.Reason,
                    Requestid = model.RequestId.ToString(),
                    Createddate = DateTime.Now,
                    Isactive = true,
                };

                _unitOfWork.BlockRequestRepo.Add(blockrequest);
                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Blocked";
                return Redirect("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while blocking request.";
                return Redirect("/Admin/Dashboard");
            }
        }

        [HttpGet]
        public IActionResult ClearCaseModal(int requestId)
        {
            ClearCaseModel model = new ClearCaseModel()
            {
                RequestId = requestId,
            };
            return PartialView("Modals/ClearCaseModal", model);
        }

        [HttpPost]
        public IActionResult ClearCaseModal(ClearCaseModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value;

            if (adminId != null)
            {
                try
                {
                    DateTime currentTime = DateTime.Now;

                    Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == model.RequestId);

                    req.Status = (short)RequestStatus.Clear;
                    req.Modifieddate = currentTime;

                    string logNotes = adminName + " cleared this request on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss");

                    Requeststatuslog reqStatusLog = new Requeststatuslog()
                    {
                        Requestid = model.RequestId,
                        Status = (short)RequestStatus.Clear,
                        Adminid = adminId,
                        Notes = logNotes,
                        Createddate = currentTime,
                    };

                    _unitOfWork.RequestRepository.Update(req);
                    _unitOfWork.Save();

                    _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                    _unitOfWork.Save();

                    TempData["success"] = "Request Successfully Cleared";
                    return Redirect("/Admin/Dashboard");
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Error Occured while clearign request.";
                    return Redirect("/Admin/Dashboard");
                }
            }
            else
            {
                TempData["error"] = "Admin Not Found";
                return Redirect("/Admin/Dashboard");
            }
        }


        [HttpGet]
        public IActionResult SendAgreementModal(int requestId, int requestType, string phone, string email)
        {
            SendAgreementModel model = new SendAgreementModel()
            {
                RequestId = requestId,
                RequestType = requestType,
                PatientPhone = phone,
                PatientEmail = email,
            };
            return PartialView("Modals/SendAgreementModal", model);
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

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = "Set up your Account",
                    IsBodyHtml = true,
                    Body = "<h1>Hello , Patient!!</h1><p>You can review your agrrement and accept it to go ahead with the medical process, which  sent by the physician. </p><a href=\"" + agreementLink + "\" >Click here to accept agreement</a>",

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

        [HttpGet]
        public IActionResult SendLinkModal()
        {
            return PartialView("Modals/SendLinkModal");
        }

        [HttpPost]
        public IActionResult SendLinkForCreateRequest(SendLinkModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string sendPatientLink = Url.Action("SubmitRequest", "Guest", new { }, Request.Scheme);

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

                    MailMessage mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, "HalloDoc"),
                        Subject = "Set up your Account",
                        IsBodyHtml = true,
                        Body = "<h1>Hola , " + model.FirstName + " " + model.LastName + "!!</h1><p>Clink the link below to create request.</p><a href=\"" + sendPatientLink + "\" >Submit Request Link</a>",

                    };

                    mailMessage.To.Add(model.Email);

                    client.Send(mailMessage);

                    return Redirect("/Admin/Dashboard");
                }
                catch (Exception e)
                {
                    TempData["error"] = "Error occurred : " + e.Message;
                    return Redirect("/Admin/Dashboard");
                }
            }

            TempData["error"] = "Please Fill all details for sending link.";
            return Redirect("/Admin/Dashboard");
        }


        #endregion

        #region ExportingExcel


        [HttpPost]
        public async Task<byte[]> ExportFilteredData(int status, int typeFilter, string searchFilter, int regionFilter)
        {
            int page = (int)HttpContext.Session.GetInt32("currentPage");

            int pageNumber = page;
            if (page < 1)
            {
                pageNumber = 1;
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

            PagedList<AdminRequest> pagedList = await _dashboardRepository.GetAdminRequestsAsync(filter);


            DataTable dt = GetDataTableFromList(pagedList, status);

            //Name of File
            string fileName = "Sample.xlsx";
            using (XLWorkbook wb = new XLWorkbook())
            {
                //Add DataTable in worksheet  
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    //Return xlsx Excel File  
                    return stream.ToArray();
                }
            }

            //string path = Path.Combine(_environment.WebRootPath, "export", "filter.csv");

            //using (var writer = new StreamWriter(path))

            //using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            //{
            //    csv.WriteRecords(pagedList);
            //}

            //return path;

        }

        [HttpPost]
        public FileResult Export(string GridHtml)
        {
            return File(Encoding.ASCII.GetBytes(GridHtml), "application/vnd.ms-excel", "Grid.xls");
        }

        public DataTable GetDataTableFromList(List<AdminRequest> requestList, int status)
        {

            DataTable dt = new DataTable();
            dt.TableName = "EmployeeData";

            if (status == (int)DashboardStatus.New)
            {

                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Date Of Birth", typeof(string));
                dt.Columns.Add("Requestor", typeof(string));
                dt.Columns.Add("Request Date", typeof(string));
                dt.Columns.Add("Phone", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                dt.Columns.Add("Notes", typeof(string));

                foreach (var request in requestList)
                {
                    string phone = "(Patient) " + request.PatientPhone;

                    if (request.RequestType != (int)RequestType.Patient)
                    {
                        phone = phone + " (Requestor) " + request.Phone;
                    }

                    dt.Rows.Add(request.PatientName,
                        request.Email,
                        request.DateOfBirth,
                        request.Requestor,
                        request.RequestDate,
                        phone,
                        request.Address,
                        request.Notes
                        );
                }

            }
            else if (status == (int)DashboardStatus.Pending || status == (int)DashboardStatus.Active)
            {

                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Date Of Birth", typeof(string));
                dt.Columns.Add("Requestor", typeof(string));
                dt.Columns.Add("Physician Name", typeof(string));
                dt.Columns.Add("Date Of Service", typeof(string));
                dt.Columns.Add("Phone", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                dt.Columns.Add("Notes", typeof(string));

                foreach (var request in requestList)
                {
                    string phone = "(Patient) " + request.PatientPhone;

                    if (request.RequestType != (int)RequestType.Patient)
                    {
                        phone = phone + " (Requestor) " + request.Phone;
                    }

                    dt.Rows.Add(request.PatientName,
                        request.Email,
                        request.DateOfBirth,
                        request.Requestor,
                        request.PhysicianName,
                        request.DateOfService,
                        phone,
                        request.Address,
                        request.Notes
                        );
                }

            }
            else if (status == (int)DashboardStatus.Conclude)
            {

                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Date Of Birth", typeof(string));
                dt.Columns.Add("Physician Name", typeof(string));
                dt.Columns.Add("Date Of Service", typeof(string));
                dt.Columns.Add("Phone", typeof(string));
                dt.Columns.Add("Address", typeof(string));

                foreach (var request in requestList)
                {
                    string phone = "(Patient) " + request.PatientPhone;

                    if (request.RequestType != (int)RequestType.Patient)
                    {
                        phone = phone + " (Requestor) " + request.Phone;
                    }

                    dt.Rows.Add(request.PatientName,
                        request.Email,
                        request.DateOfBirth,
                        request.PhysicianName,
                        request.DateOfService,
                        phone,
                        request.Address
                        );
                }
            }
            else if (status == (int)DashboardStatus.ToClose)
            {

                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Date Of Birth", typeof(string));
                dt.Columns.Add("Region", typeof(string));
                dt.Columns.Add("Physician Name", typeof(string));
                dt.Columns.Add("Date Of Service", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                dt.Columns.Add("Notes", typeof(string));

                foreach (var request in requestList)
                {
                    dt.Rows.Add(request.PatientName,
                        request.Email,
                        request.DateOfBirth,
                        request.RegionName,
                        request.PhysicianName,
                        request.DateOfService,
                        request.Address,
                        request.Notes
                        );
                }

            }
            else if (status == (int)DashboardStatus.Unpaid)
            {

                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Physician Name", typeof(string));
                dt.Columns.Add("Date Of Service", typeof(string));
                dt.Columns.Add("Phone", typeof(string));
                dt.Columns.Add("Address", typeof(string));

                foreach (var request in requestList)
                {
                    string phone = "(Patient) " + request.PatientPhone;

                    if (request.RequestType != (int)RequestType.Patient)
                    {
                        phone = phone + " (Requestor) " + request.Phone;
                    }

                    dt.Rows.Add(request.PatientName,
                        request.Email,
                        request.PhysicianName,
                        request.DateOfService,
                        phone,
                        request.Address
                        );
                }

            }


            dt.AcceptChanges();

            return dt;
        }

        public byte[] ExportAllExcel(int status)
        {
            IEnumerable<AdminRequest> allRequest = _dashboardRepository.GetAllRequestByStatus(status);

            DataTable dt = GetDataTableFromList(allRequest.ToList(), status);
            //Name of File
            string fileName = "Sample.xlsx";
            using (XLWorkbook wb = new XLWorkbook())
            {
                //Add DataTable in worksheet  
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    //Return xlsx Excel File  
                    return stream.ToArray();
                }
            }

        }

        [HttpPost]
        public string ExportAll(int status)
        {
            IEnumerable<AdminRequest> allRequest = _dashboardRepository.GetAllRequestByStatus(status);

            string path = Path.Combine(_environment.WebRootPath, "export", "sample.csv");

            using (var writer = new StreamWriter(path))

            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(allRequest);
            }

            return "export/sample.csv";

        }

        #endregion


        public IActionResult CreateRequest()
        {
            IEnumerable<Region> regions = _unitOfWork.RegionRepository.GetAll();
            AdminCreateRequestViewModel model = new AdminCreateRequestViewModel();
            model.regions = regions;
            return View("Dashboard/CreateRequest", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRequest(AdminCreateRequestViewModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);
            if (adminId != 0)
            {
                if (ModelState.IsValid)
                {
                    var phoneNumber = "+" + model.countryCode + "-" + model.phoneNumber;
                    string state = _unitOfWork.RegionRepository.GetFirstOrDefault(reg => reg.Regionid == model.state).Name;

                    // Creating Patient in Aspnetusers Table

                    bool isUserExists = _unitOfWork.UserRepository.IsUserWithEmailExists(model.email);

                    if (!isUserExists)
                    {

                        Guid generatedId = Guid.NewGuid();

                        Aspnetuser aspnetuser = new()
                        {
                            Id = generatedId.ToString(),
                            Username = model.FirstName,
                            Passwordhash = null,
                            Email = model.email,
                            Phonenumber = phoneNumber,
                            Createddate = DateTime.Now
                        };

                        _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                        _unitOfWork.Save();

                        User user1 = new()
                        {
                            Aspnetuserid = generatedId.ToString(),
                            Firstname = model.FirstName,
                            Lastname = model.LastName,
                            Email = model.email,
                            Mobile = phoneNumber,
                            Street = model.street,
                            City = model.city,
                            State = state,
                            Regionid = model.state,
                            Zipcode = model.zipCode,
                            Createddate = DateTime.Now,
                            Createdby = admin.Aspnetuserid,
                            Strmonth = model.dob?.Month.ToString(),
                            Intdate = model.dob?.Day,
                            Intyear = model.dob?.Year
                        };


                        _unitOfWork.UserRepository.Add(user1);
                        _unitOfWork.Save();

                        SendMailForCreateAccount(model.email);
                    }

                    User user = _unitOfWork.UserRepository.GetFirstOrDefault(u => u.Email == model.email);

                    // Adding request in Request Table
                    Request request = new()
                    {
                        Requesttypeid = 2,
                        Userid = user.Userid,
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Phonenumber = phoneNumber,
                        Email = model.email,
                        Status = (short)RequestStatus.Unassigned,
                        Createddate = DateTime.Now,
                        Confirmationnumber = GenerateConfirmationNumber(user),
                        Patientaccountid = user.Aspnetuserid,
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
                        Email = model.email,
                        Address = model.street,
                        City = model.city,
                        State = state,
                        Regionid = model.state,
                        Zipcode = model.zipCode,
                        Strmonth = model.dob?.Month.ToString(),
                        Intdate = model.dob?.Day,
                        Intyear = model.dob?.Year
                    };

                    _unitOfWork.RequestClientRepository.Add(requestclient);
                    _unitOfWork.Save();
                    if (model.notes != null)
                    {
                        Requestnote rn = new()
                        {
                            Requestid = request.Requestid,
                            Physiciannotes = null,
                            Adminnotes = model.notes,
                            Createdby = admin.Aspnetuserid,
                            Createddate = DateTime.Now
                        };
                        _unitOfWork.RequestNoteRepository.Add(rn);
                        _unitOfWork.Save();
                    }

                    model.regions = _unitOfWork.RegionRepository.GetAll();
                    return View("Dashboard/CreateRequest", model);

                }
                return View("error");
            }
            return View("error");

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
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(ad => ad.Adminid == adminId);

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
                    Createdby = admin.Aspnetuserid,
                };

                _unitOfWork.OrderDetailRepo.Add(order);
                _unitOfWork.Save();

                TempData["success"] = "Order Successfully Sent";

            }
            else
            {
                TempData["error"] = "Error occured whlie ordering.";
            }

            return Orders(orderViewModel.RequestId);
        }

        public async Task<IActionResult> DownloadAllFiles(int requestId)
        {
            try
            {
                // Fetch all document details for the given request:
                var documentDetails = _unitOfWork.RequestWiseFileRepository.Where(m => m.Requestid == requestId && m.Isdeleted != true).ToList();

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

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult ViewCase(int Requestid)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            if (Requestid == null)
            {
                return View("Error");
            }

            Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == Requestid);
            Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqCli => reqCli.Requestid == Requestid);

            ViewCaseViewModel model = new();

            model.UserName = adminName;

            string dobDate = client.Intyear + "-" + client.Strmonth + "-" + client.Intdate;
            model.Confirmation = req.Confirmationnumber;
            model.DashboardStatus = GetDashboardStatus(req.Status);
            model.RequestType = req.Requesttypeid;
            model.PatientName = client.Firstname + " " + client.Lastname;
            model.PatientFirstName = client.Firstname;
            model.PatientLastName = client.Lastname;
            model.Dob = dobDate == "--" ? null : DateTime.Parse(dobDate);
            model.PatientEmail = client.Email;
            model.Region = client.Regionid;
            model.Notes = client.Notes;
            model.Address = client.Street;
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.physicians = _unitOfWork.PhysicianRepository.GetAll();
            model.casetags = _unitOfWork.CaseTagRepository.GetAll();

            return View("Dashboard/ViewCase", model);
        }

        public int GetDashboardStatus(int requestStatus)
        {
            switch (requestStatus)
            {
                case (int)RequestStatus.Unassigned:
                    return (int)DashboardStatus.New;
                case (int)RequestStatus.Accepted:
                    return (int)DashboardStatus.Pending;
                case (int)RequestStatus.MDOnSite:
                case (int)RequestStatus.MDEnRoute:
                    return (int)DashboardStatus.Active;
                case (int)RequestStatus.Conclude:
                    return (int)DashboardStatus.Conclude;
                case (int)RequestStatus.Cancelled:
                case (int)RequestStatus.Closed:
                case (int)RequestStatus.CancelledByPatient:
                    return (int)DashboardStatus.ToClose;
                case (int)RequestStatus.Unpaid:
                    return (int)DashboardStatus.Unpaid;
                default: return -1;
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ViewCase(ViewCaseViewModel viewCase)
        {

            if (viewCase != null)
            {

                string phoneNumber = "+" + viewCase.CountryCode + '-' + viewCase.PatientPhone;

                Requestclient reqcli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(req => req.Requestid == viewCase.RequestId);
                reqcli.Notes = viewCase.Notes;

                _unitOfWork.RequestClientRepository.Update(reqcli);
                _unitOfWork.Save();

                return ViewCase(viewCase.RequestId);

            }
            return View("Error");

        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult ViewNotes(int Requestid)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            IEnumerable<Requeststatuslog> statusLogs = _unitOfWork.RequestStatusLogRepository.Where(log => log.Requestid == Requestid).OrderBy(_ => _.Createddate);

            Requestnote notes = _unitOfWork.RequestNoteRepository.GetFirstOrDefault(notes => notes.Requestid == Requestid);

            ViewNotesViewModel model = new ViewNotesViewModel();

            model.UserName = adminName;
            model.requeststatuslogs = statusLogs;

            if (notes != null)
            {
                model.AdminNotes = notes.Adminnotes;
                model.PhysicianNotes = notes.Physiciannotes;
            }

            return View("Dashboard/ViewNotes", model);
        }

        [HttpPost]
        public IActionResult ViewNotes(ViewNotesViewModel vnvm)
        {

            int adminId = 1;
            string adminAspId = "061d38d4-2b2f-48f6-ad21-5a80db6c4e69";
            Requestnote oldnote = _unitOfWork.RequestNoteRepository.GetFirstOrDefault(rn => rn.Requestid == vnvm.RequestId);

            if (oldnote != null)
            {
                oldnote.Adminnotes = vnvm.AdminNotes;
                oldnote.Modifieddate = DateTime.Now;
                oldnote.Modifiedby = adminAspId;

                _unitOfWork.RequestNoteRepository.Update(oldnote);
                _unitOfWork.Save();

            }
            else
            {
                Requestnote curReqNote = new Requestnote()
                {
                    Requestid = vnvm.RequestId,
                    Adminnotes = vnvm.AdminNotes,
                    Createdby = adminAspId,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestNoteRepository.Add(curReqNote);
                _unitOfWork.Save();

            }

            return ViewNotes(vnvm.RequestId);
        }

        public IActionResult ViewUploads(int Requestid)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == Requestid);
            if (req == null)
            {
                return View("Error");
            }

            Requestclient reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == req.Requestid);
            if (reqCli == null)
            {
                return View("Error");
            }

            List<Requestwisefile> files = _unitOfWork.RequestWiseFileRepository.Where(file => file.Requestid == Requestid && file.Isdeleted != true).ToList();

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
                UserName = adminName
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

        [HttpPost]
        public IActionResult ViewUploads(ViewUploadsViewModel uploadsVM)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (uploadsVM.File != null)
            {
                InsertRequestWiseFile(uploadsVM.File);

                Requestwisefile requestwisefile = new()
                {
                    Requestid = uploadsVM.RequestId,
                    Filename = uploadsVM.File.FileName,
                    Createddate = DateTime.Now,
                    Adminid = adminId,
                    Ip = "127.0.0.1",
                };

                _unitOfWork.RequestWiseFileRepository.Add(requestwisefile);
                _unitOfWork.Save();

                uploadsVM.File = null;

            }
            return ViewUploads(uploadsVM.RequestId);
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
                Requestclient reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(requestCli => requestCli.Requestid == requestId);

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
                    Requestwisefile file = _unitOfWork.RequestWiseFileRepository.GetFirstOrDefault(reqFile => reqFile.Requestwisefileid == fileId);
                    string documentPath = Path.Combine(_environment.WebRootPath, "document", file.Filename);

                    byte[] fileBytes = System.IO.File.ReadAllBytes(documentPath);
                    memoryStream = new MemoryStream(fileBytes);
                    mailMessage.Attachments.Add(new Attachment(memoryStream, file.Filename));
                }

                mailMessage.To.Add(reqCli.Email);

                client.Send(mailMessage);

                TempData["success"] = "Email with selected documents has been successfully sent to " + reqCli.Email;
                return true;
            }
            catch (Exception e)
            {
                TempData["error"] = "Error occured while sending documents. Please try again later.";
                return false;
            }
        }

        public IActionResult CloseCase(int requestid)
        {
            var requestClient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(s => s.Requestid == requestid);
            var docData = _unitOfWork.RequestWiseFileRepository.Where(s => s.Requestid == requestid).ToList();
            var Confirmationnum = _unitOfWork.RequestRepository.GetFirstOrDefault(s => s.Requestid == requestid).Confirmationnumber;

            string dobDate = null;
            if (requestClient.Intyear != null || requestClient.Intdate != null || requestClient.Strmonth != null)
            {
                dobDate = requestClient.Intyear + "-" + requestClient.Strmonth + "-" + requestClient.Intdate;
            }

            if (docData != null && requestClient != null)
            {
                CloseCaseViewModel closeCase = new CloseCaseViewModel
                {
                    FirstName = requestClient.Firstname,
                    LastName = requestClient.Lastname,
                    Email = requestClient.Email,
                    PhoneNumber = requestClient.Phonenumber,
                    Dob = dobDate != null ? DateTime.Parse(dobDate) : null,
                    Files = docData,
                    requestid = requestid,
                    confirmatioNumber = Confirmationnum,
                };
                return View("Dashboard/CloseCase", closeCase);
            }

            return Ok();
        }


        [HttpPost]
        public IActionResult CloseCase(CloseCaseViewModel closeCase, int id)
        {
            var reqclient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(s => s.Requestid == id);

            if (reqclient != null)
            {
                reqclient.Phonenumber = closeCase.PhoneNumber;
                reqclient.Firstname = closeCase.FirstName;
                reqclient.Lastname = closeCase.LastName;
                reqclient.Intdate = closeCase.Dob?.Day;
                reqclient.Intyear = closeCase.Dob?.Year;
                reqclient.Strmonth = closeCase.Dob?.Month.ToString();

                _unitOfWork.RequestClientRepository.Update(reqclient);
                _unitOfWork.Save();

            }
            return RedirectToAction("CloseCase", new { requestid = id });
        }

        public IActionResult CloseInstance(int reqid)
        {

            DateTime currentdate = DateTime.Now;
            string adminName = HttpContext.Request.Headers.Where(a => a.Key == "userName").FirstOrDefault().Value;

            Requestclient reqclient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(s => s.Requestid == reqid);
            Request request = _unitOfWork.RequestRepository.GetFirstOrDefault(r => r.Requestid == reqid);

            if (request != null)
            {
                request.Status = 9;
                request.Modifieddate = DateTime.Now;
                _unitOfWork.RequestRepository.Update(request);
                _unitOfWork.Save();

                Requeststatuslog requestStatusLog = new Requeststatuslog();
                requestStatusLog.Requestid = reqid;
                requestStatusLog.Status = (short)RequestStatus.Unpaid;
                requestStatusLog.Notes = adminName + " closed this request on " + currentdate.ToString("MM/dd/yyyy") + " at " + currentdate.ToString("HH:mm:ss");
                requestStatusLog.Createddate = DateTime.Now;

                _unitOfWork.RequestStatusLogRepository.Add(requestStatusLog);
                _unitOfWork.Save();
                return RedirectToAction("Dashboard");
            }
            return Ok();
        }

        public IActionResult EncounterForm(int requestid)
        {
            Encounterform encounterform = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(e => e.Requestid == requestid);
            Requestclient requestclient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(e => e.Requestid == requestid);
            Request request = _unitOfWork.RequestRepository.GetFirstOrDefault(r => r.Requestid == requestid);
            string dobDate = null;

            if (requestclient.Intyear != null && requestclient.Strmonth != null && requestclient.Intdate != null)
            {
                dobDate = requestclient.Intyear + "-" + requestclient.Strmonth + "-" + requestclient.Intdate;
            }

            EncounterFormViewModel encounterViewModel;

            if (encounterform != null)
            {

                encounterViewModel = new()
                {
                    FirstName = requestclient.Firstname,
                    LastName = requestclient.Lastname,
                    Email = requestclient.Email,
                    PhoneNumber = requestclient.Phonenumber,
                    DOB = dobDate != null ? DateTime.Parse(dobDate) : null,
                    CreatedDate = request.Createddate,
                    Location = requestclient.Street + " " + requestclient.City + " " + requestclient.State,
                    MedicalHistory = encounterform.Medicalhistory,
                    History = encounterform.Historyofpresentillnessorinjury,
                    Medications = encounterform.Medications,
                    Allergies = encounterform.Allergies,
                    Temp = encounterform.Temp,
                    HR = encounterform.Hr,
                    RR = encounterform.Rr,
                    BpLow = encounterform.Bloodpressuresystolic,
                    BpHigh = encounterform.Bloodpressuresystolic,
                    O2 = encounterform.O2,
                    Pain = encounterform.Pain,
                    Heent = encounterform.Heent,
                    CV = encounterform.Cv,
                    Chest = encounterform.Chest,
                    ABD = encounterform.Abd,
                    Extr = encounterform.Extremities,
                    Skin = encounterform.Skin,
                    Neuro = encounterform.Neuro,
                    Other = encounterform.Other,
                    Diagnosis = encounterform.Diagnosis,
                    TreatmentPlan = encounterform.TreatmentPlan,
                    Procedures = encounterform.Procedures,
                    MedicationDispensed = encounterform.Medicaldispensed,
                    FollowUps = encounterform.Followup

                };

                return View("Dashboard/EncounterForm", encounterViewModel);

            }

            encounterViewModel = new()
            {
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
            string adminName = HttpContext.Request.Headers.Where(a => a.Key == "userName").FirstOrDefault().Value;
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Firstname + " " + a.Lastname == adminName);
            Request r = _unitOfWork.RequestRepository.GetFirstOrDefault(rs => rs.Requestid == model.RequestId);
            if (ModelState.IsValid)
            {
                Encounterform encounterform = _unitOfWork.EncounterFormRepository.GetFirstOrDefault(e => e.Requestid == model.RequestId);
                if (encounterform == null)
                {
                    Encounterform encf = new()
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
                        Adminid = admin.Adminid,
                        Physicianid = r.Physicianid,
                        Isfinalize = false

                    };
                    _unitOfWork.EncounterFormRepository.Add(encf);
                    _unitOfWork.Save();
                }
                else
                {
                    encounterform.Requestid = model.RequestId;
                    encounterform.Historyofpresentillnessorinjury = model.History;
                    encounterform.Medicalhistory = model.MedicalHistory;
                    encounterform.Medications = model.Medications;
                    encounterform.Allergies = model.Allergies;
                    encounterform.Temp = model.Temp;
                    encounterform.Hr = model.HR;
                    encounterform.Rr = model.RR;
                    encounterform.Bloodpressuresystolic = model.BpLow;
                    encounterform.Bloodpressurediastolic = model.BpHigh;
                    encounterform.O2 = model.O2;
                    encounterform.Pain = model.Pain;
                    encounterform.Skin = model.Skin;
                    encounterform.Heent = model.Heent;
                    encounterform.Neuro = model.Neuro;
                    encounterform.Other = model.Other;
                    encounterform.Cv = model.CV;
                    encounterform.Chest = model.Chest;
                    encounterform.Abd = model.ABD;
                    encounterform.Extremities = model.Extr;
                    encounterform.Diagnosis = model.Diagnosis;
                    encounterform.TreatmentPlan = model.TreatmentPlan;
                    encounterform.Procedures = model.Procedures;
                    encounterform.Adminid = admin.Adminid;
                    encounterform.Physicianid = r.Physicianid;
                    encounterform.Isfinalize = false;

                    _unitOfWork.EncounterFormRepository.Update(encounterform);
                    _unitOfWork.Save();
                }
                return EncounterForm((int)model.RequestId);
            }
            return View("error");
        }


        #endregion

        #region Providers

        public IActionResult Scheduling()
        {
            SchedulingViewModel model = new SchedulingViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();

            return View("Providers/Scheduling", model);
        }
        [HttpPost]
        public List<Physician> GetPhyByRegion(int id)
        {
            return _context.Physicians.Where(a => a.Regionid == id).ToList();
        }
        public enum shiftStatus
        {
            Pending = 2,
            Approved = 1
        }
        public enum shiftWeekDays
        {
            Sunday = 1,
            Monday = 2,
            Tuesday = 3,
            Wednesday = 4,
            Thursday = 5,
            Friday = 6,
            Saturday = 7
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

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin admin = _context.Admins.FirstOrDefault(a => a.Adminid == adminId);
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
                shift.Createdby = admin.Aspnetuserid;
                _context.Shifts.Add(shift);
                _context.SaveChanges();

                Shiftdetail shiftdetail1 = new()
                {
                    Shiftid = shift.Shiftid,
                    Shiftdate = (DateTime)model.shiftDate,
                    Regionid = model.addShiftRegion,
                    Starttime = (TimeOnly)model.shiftStartTime,
                    Endtime = (TimeOnly)model.shiftEndTime,
                    Status = (short)shiftStatus.Approved,
                    Isdeleted = false
                };
                _context.Shiftdetails.Add(shiftdetail1);
                _context.SaveChanges();

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
                            Status = (short)shiftStatus.Approved,
                            Isdeleted = false
                        };

                        _context.Shiftdetails.Add(shiftdetail);
                        _context.SaveChanges();

                        Shiftdetailregion s = new()
                        {
                            Shiftdetailid = shiftdetail.Shiftdetailid,
                            Regionid = (int)shiftdetail.Regionid
                        };
                        _context.Shiftdetailregions.Add(s);
                        _context.SaveChanges();
                    }

                    currentDate = GetNextWeekday(currentDate, 7); // Move to next week
                }
            }

            return RedirectToAction("Scheduling");
        }

        private DateTime GetNextWeekday(DateTime startDate, int targetDayOfWeek)
        {
            int currentDayOfWeek = (int)startDate.DayOfWeek;
            int daysToAdd = targetDayOfWeek - currentDayOfWeek;

            if (daysToAdd <= 0) daysToAdd += 7; // If the target day is earlier in the week, move to next week

            return startDate.AddDays(daysToAdd);
        }

        public IActionResult DaySchedule()
        {
            // Assume you have a list of ShiftViewModel objects populated from your database
            List<string> hours = GenerateHours();

            ShiftTableViewModel viewModel = new()
            {
                Hours = hours,
            };

            return PartialView("ScheduleDayWiseTable", viewModel);
        }


        private List<string> GenerateHours()
        {
            List<string> hours = new List<string>();

            // Generate hours from 12 AM to 11 PM
            for (int i = 0; i < 24; i++)
            {
                string hour = (i % 12 == 0 ? "12" : (i % 12).ToString()) + (i < 12 ? "A" : "P");
                hours.Add(hour);
            }

            return hours;
        }

        private List<string> GenerateHourTime()
        {
            List<string> hours = new List<string>();

            // Generate hours from 12 AM to 11 PM
            for (int i = 0; i < 24; i++)
            {
                string hour = ((i).ToString()) + ":00:00";
                hours.Add(hour);
            }

            return hours;
        }
        
        [HttpPost]
        public IActionResult EditShift(int shiftId, TimeOnly startTime, TimeOnly endTime, DateTime shiftdate)
        {
            Shiftdetail sd = _context.Shiftdetails.FirstOrDefault(s => s.Shiftdetailid == shiftId);
            if (sd != null)
            {

                sd.Starttime = startTime;
                sd.Endtime = endTime;
                sd.Shiftdate = shiftdate;
            }
            _context.Shiftdetails.Update(sd);
            _context.SaveChanges();
            return RedirectToAction("SchedulingHeader");
        }

        private List<ShiftViewModel> GetShiftViewModels(DateTime currentDate)
        {
            // Retrieve shift details from the database or any other data source

            var query = (from s in _context.Shifts
                         join sd in _context.Shiftdetails on s.Shiftid equals sd.Shiftid
                         join p in _context.Physicians on s.Physicianid equals p.Physicianid into subgroup
                         from subitem in subgroup.DefaultIfEmpty()
                         select new ShiftViewModel
                         {
                             ProviderName = subitem.Firstname + " " + subitem.Lastname,
                             PhysicianId = subitem.Physicianid,
                             StartTime = sd.Starttime,
                             EndTime = sd.Endtime,
                             Status = sd.Status,
                             shiftDate = sd.Shiftdate

                         }).Where(x => x.shiftDate == currentDate);

            return query.ToList();
        }

        [HttpPost]
        public IActionResult ScheduleMonthWisePartial(int shiftMonth, int shiftYear)
        {
            //string startDate = shiftYear + "-" + (shiftMonth + 1) + "-01";

            //int days = DateTime.DaysInMonth(shiftYear, (shiftMonth + 1));

            //int startDayOfWeek = (int)DateTime.Parse(startDate).DayOfWeek;

            var query = _context.Shiftdetails.Where(shift => shift.Shiftdate.Month == (shiftMonth + 1) && shift.Shiftdate.Year == shiftYear);

            //var query = (from s in _context.Shifts
            //             join sd in _context.Shiftdetails
            //             on s.Shiftid equals sd.Shiftid
            //             where sd.Shiftdate.Month == (shiftMonth + 1)
            //             join p in _context.Physicians on s.Physicianid equals p.Physicianid into subgroup
            //             from subitem in subgroup.DefaultIfEmpty()
            //             select new ShiftItem
            //             {
            //                 PhysicianId = sd.Shiftdetailid,
            //                 RegionId = sd.Regionid ?? 0,
            //                 PhysicianName = subitem.Firstname + " " + subitem.Lastname,
            //                 StartTime = sd.Starttime,
            //                 EndTime = sd.Endtime,
            //                 Status = sd.Status,
            //                 ShiftDate = sd.Shiftdate,
            //             });

            ShiftMonthViewModel model = new ShiftMonthViewModel()
            {
                shiftDetails = query,
            };

            return PartialView("Partial/ScheduleMonthWiseTable", model);
        }

        [HttpPost]
        public IActionResult ScheduleWeekWisePartial(DateTime startDate)
        {

            DateTime start = startDate.ToLocalTime();
            DateTime end = start.AddDays(7);

            ShiftWeekViewModel model = new ShiftWeekViewModel();

            //var query = (from p in _context.Physicians
            //             select new PhysicianShift
            //             {
            //                 PhysicianId = p.Physicianid,
            //                 PhysicianName = p.Firstname,
            //                 shiftDetails = GetPhyShiftDetails(startDate,p.Physicianid)
            //             });

            //var query = (from p in _context.Physicians
            //             join s in _context.Shifts on p.Physicianid equals s.Physicianid into shiftGroup
            //             from shiftItem in shiftGroup.DefaultIfEmpty()
            //             join sd in _context.Shiftdetails on shiftItem.Shiftid equals sd.Shiftid into detailGroup
            //             from detailItem in detailGroup.DefaultIfEmpty()
            //             where (detailItem.Shiftdate <= end)
            //             group detailItem by p.Physicianid into sdGroup
            //             select new PhysicianShift
            //             {
            //                 PhysicianId = sdGroup.Key,
            //                 shiftDetails = sdGroup.ToList(),
            //             });

            //var query = (from p in _context.Physicians
            //             join s in _context.Shifts on p.Physicianid equals s.Physicianid into shiftGroup
            //             from shiftItem in shiftGroup.DefaultIfEmpty()
            //             join sd in _context.Shiftdetails on shiftItem.Shiftid equals sd.Shiftid 
            //             where (sd.Shiftdate <= end)
            //             group sd by p.Physicianid into sdGroup
            //             select new PhysicianShift
            //             {
            //                 PhysicianId = sdGroup.Key,
            //                 shiftDetails = sdGroup.ToList(),
            //             });

            //var query = (from p in _context.Physicians
            //             join sgroup in
            //             (from s in _context.Shifts
            //             join sd in _context.Shiftdetails on s.Shiftid equals sd.Shiftid) on p.Physicianid equals sgroup.Physicianid into phyGroup
            //             from phyItem in 
            //             where (sd.Shiftdate <= end)
            //             group sd by p.Physicianid into sdGroup
            //             select new PhysicianShift
            //             {
            //                 PhysicianId = sdGroup.Key,
            //                 shiftDetails = sdGroup.ToList(),
            //             });

            IEnumerable<Physician> phyList = _context.Physicians.ToList();
            List<PhysicianShift> physicianShifts = new List<PhysicianShift>();


      

            foreach (var phy in phyList)
            {
                var query = (from sd in _context.Shiftdetails
                             join s in _context.Shifts on sd.Shiftid equals s.Shiftid
                             where (s.Physicianid == phy.Physicianid && sd.Shiftdate >= start && sd.Shiftdate <= end)
                             select new Shiftdetail
                             {
                                 Shiftid = s.Shiftid,
                                 Starttime = sd.Starttime,
                                 Endtime = sd.Endtime,
                             });

                PhysicianShift shift = new()
                {
                    PhysicianId = phy.Physicianid,
                    PhysicianName = phy.Firstname,
                    shiftDetails = query,
                };

                physicianShifts.Add(shift);
            }

            model.physicianShifts = physicianShifts;
            model.physicians = phyList;

            return PartialView("Partial/ScheduleWeekWiseTable", model);
        }

        public IEnumerable<Shiftdetail> GetPhyShiftDetails(DateTime startDate, int physicianId)
        {

            DateTime start = startDate.ToLocalTime();
            DateTime end = start.AddDays(7);

            var query = (from s in _context.Shifts
                         join sd in _context.Shiftdetails on s.Shiftid equals sd.Shiftid
                         join p in _context.Physicians on s.Physicianid equals p.Physicianid
                         where (p.Physicianid == physicianId && sd.Shiftdate >= start && sd.Shiftdate <= end)
                         select new Shiftdetail
                         {
                             Starttime = sd.Starttime,
                             Endtime = sd.Endtime,
                         });

            return query;
        }

        [HttpPost]
        public IActionResult SchedulePartialTable(int shiftStatus, int typeFilter, DateTime dayFilter, int regionFilter, DateTime startDate)
        {
            if (dayFilter != null)
            {
                //    DateTime myDate = DateTime.ParseExact("2024-03-31 00:00:00,000", "yyyy-MM-dd HH:mm:ss,fff",
                //                           System.Globalization.CultureInfo.InvariantCulture);

                DateTime currentDate = startDate.ToLocalTime();
                List<ShiftViewModel> shiftViewModels = GetShiftViewModels(currentDate);
                List<string> hours = GenerateHours();
                List<string> hourtimes = GenerateHourTime();

                ShiftTableViewModel viewModel = new()
                {
                    hourTime = hourtimes,
                    Hours = hours,
                    Shifts = shiftViewModels,
                    physicians = _context.Physicians,
                };
                return PartialView("Partial/ScheduleDayWiseTable", viewModel);

            }
            else
            {
                return View("error");
            }
        }


        public void InsertFileAfterRename(IFormFile file, string path, string updateName)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string[] oldfiles = Directory.GetFiles(path, updateName + ".*");
            foreach (string f in oldfiles)
            {
                System.IO.File.Delete(f);
            }

            string extension = Path.GetExtension(file.FileName);

            string fileName = updateName + extension;

            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            file.CopyTo(stream);

        }

        [HttpPost]
        public bool SavePhysicianProfileInfo(int PhysicianId, IFormFile Signature, IFormFile Photo, string BusinessName, string BusinessWebsite)
        {

            List<string> validProfileExtensions = new List<string> { ".jpeg", ".png", ".jpg" };
            List<string> validDocumentExtensions = new List<string> { ".pdf" };
            if (PhysicianId == null || PhysicianId == 0)
            {
                return false;
            }

            try
            {

                string path = Path.Combine(_environment.WebRootPath, "document", "physician", PhysicianId.ToString());
                Physician physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == PhysicianId);

                physician.Businessname = BusinessName;
                physician.Businesswebsite = BusinessWebsite;

                if (Signature != null)
                {
                    string sigExtension = Path.GetExtension(Signature.FileName);

                    if (!validProfileExtensions.Contains(sigExtension))
                    {
                        TempData["error"] = "Invalid Signature Extension";
                        return false;
                    }
                    InsertFileAfterRename(Signature, path, "Signature");

                    physician.Signature = Signature.FileName;
                }

                if (Photo != null)
                {
                    string profileExtension = Path.GetExtension(Photo.FileName);

                    if (!validProfileExtensions.Contains(profileExtension))
                    {
                        TempData["error"] = "Invalid Profile Photo Extension";
                        return false;
                    }
                    InsertFileAfterRename(Photo, path, "ProfilePhoto");
                    physician.Photo = Photo.FileName;

                }

                _unitOfWork.PhysicianRepository.Update(physician);
                _unitOfWork.Save();

                TempData["success"] = "Data updated successfully.";
                return true;
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return false;
            }

        }

        [HttpPost]
        public bool SavePhysicianOnboardingInfo(int PhysicianId, IFormFile ICA, IFormFile BGCheck, IFormFile HIPAACompliance, IFormFile NDA, IFormFile LicenseDoc)
        {


            if (PhysicianId == null || PhysicianId == 0)
            {
                return false;
            }

            try
            {
                string path = Path.Combine(_environment.WebRootPath, "document", "physician", PhysicianId.ToString());
                Physician phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == PhysicianId);

                if (ICA != null)
                {
                    InsertFileAfterRename(ICA, path, "ICA");
                    phy.Isagreementdoc = true;
                }


                if (BGCheck != null)
                {
                    InsertFileAfterRename(BGCheck, path, "BackgroundCheck");
                    phy.Isbackgrounddoc = true;
                }


                if (HIPAACompliance != null)
                {
                    InsertFileAfterRename(HIPAACompliance, path, "HipaaCompliance");
                    phy.Iscredentialdoc = true;
                }


                if (NDA != null)
                {
                    InsertFileAfterRename(NDA, path, "NDA");
                    phy.Isnondisclosuredoc = true;
                }


                if (LicenseDoc != null)
                {
                    InsertFileAfterRename(LicenseDoc, path, "LicenseDoc");
                    phy.Islicensedoc = true;
                }



                _unitOfWork.PhysicianRepository.Update(phy);
                _unitOfWork.Save();

                TempData["success"] = "Data updated successfully";
                return true;
            }
            catch (Exception e)
            {
                TempData["error"] = e.ToString();
                return false;
            }

            return false;
        }

        [HttpPost]
        public bool SavePhysicianInformation(int PhysicianId, string FirstName, string LastName, string Email, string Phone, string CountryCode, string MedicalLicenseNumber, string NPINumber, string SyncEmail, List<int> selectedRegions)
        {
            if (PhysicianId == null || PhysicianId == 0)
            {
                return false;
            }

            try
            {
                string phone = "+" + CountryCode + "-" + Phone;
                Physician phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == PhysicianId);
                phy.Firstname = FirstName;
                phy.Lastname = LastName;
                phy.Mobile = Phone;
                phy.Medicallicense = MedicalLicenseNumber;
                phy.Npinumber = NPINumber;
                phy.Syncemailaddress = SyncEmail;


                List<int> physicianRegions = _unitOfWork.PhysicianRegionRepo.Where(region => region.Physicianid == PhysicianId).ToList().Select(x => (int)x.Regionid).ToList();

                List<int> commonRegions = new List<int>();

                // Finding common regions in both new and old lists
                foreach (int region in physicianRegions)
                {
                    if (selectedRegions.Contains(region))
                    {
                        commonRegions.Add(region);
                    }
                }

                // Removing them from both lists
                foreach (int region in commonRegions)
                {
                    selectedRegions.Remove(region);
                    physicianRegions.Remove(region);
                }

                // From difference we will remove regions that were in old list but not in new list
                foreach (int region in physicianRegions)
                {
                    Physicianregion pr = _unitOfWork.PhysicianRegionRepo.GetFirstOrDefault(ar => ar.Regionid == region);
                    _unitOfWork.PhysicianRegionRepo.Remove(pr);
                }

                // And Add the regions that were in new list but not in old list
                foreach (int region in selectedRegions)
                {
                    Physicianregion phyRegion = new Physicianregion()
                    {
                        Physicianid = PhysicianId,
                        Regionid = region,
                    };

                    _unitOfWork.PhysicianRegionRepo.Add(phyRegion);
                }

                _unitOfWork.PhysicianRepository.Update(phy);
                _unitOfWork.Save();

                TempData["success"] = "Data updated successfully";
                return true;
            }
            catch (Exception e)
            {
                TempData["error"] = e.ToString();
                return false;
            }

            return false;
        }

        [HttpPost]
        public bool SavePhysicianBillingInfo(int PhysicianId, string Address1, string Address2, string City, int RegionId, string Zip, string MailCountryCode, string MailPhone)
        {
            if (PhysicianId == null || PhysicianId == 0)
            {
                return false;
            }

            try
            {
                string phone = "+" + MailCountryCode + "-" + MailPhone;
                Physician phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == PhysicianId);
                phy.Address1 = Address1;
                phy.Address2 = Address2;
                phy.City = City;
                phy.Zip = Zip;
                phy.Altphone = phone;
                phy.Regionid = RegionId;


                _unitOfWork.PhysicianRepository.Update(phy);
                _unitOfWork.Save();

                TempData["success"] = "Data updated successfully";
                return true;
            }
            catch (Exception e)
            {
                TempData["error"] = e.ToString();
                return false;
            }

        }


        public IActionResult CreatePhysicianAccount()
        {
            EditPhysicianViewModel model = new EditPhysicianViewModel()
            {
                regions = _unitOfWork.RegionRepository.GetAll(),
                roles = _context.Roles,
            };
            return View("Providers/CreatePhysicianAccount", model);
        }


        public async Task<string> FetchLatLang(EditPhysicianViewModel model)
        {
            try
            {
                string state = _unitOfWork.RegionRepository.GetFirstOrDefault(reg => reg.Regionid == model.RegionId).Name;

                using (var client = new HttpClient())
                {
                    string apiKey = _config.GetSection("Geocoding")["ApiKey"];
                    string baseUrl = $"https://geocode.maps.co/search?city={model.City}&state={state}&postalcode={model.Zip}&country=India&api_key=" + apiKey;
                    //HTTP GET

                    var responseTask = client.GetAsync(baseUrl);
                    responseTask.Wait();

                    var result = responseTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        var content = await result.Content.ReadAsStringAsync();

                        var json = JsonArray.Parse(content);

                        string? latitude = json?[0]?["lat"]?.ToString();
                        string? longitude = json?[0]?["lon"]?.ToString();

                    }
                    else
                    {
                        //log response status here

                        ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                    }
                }

            }
            catch (Exception e)
            {
                var error = e.Message;
            }


            return "bye";
        }

        [HttpPost]
        public IActionResult CreatePhysicianAccount(EditPhysicianViewModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin? admin = _context.Admins.FirstOrDefault(x => x.Adminid == adminId);

            List<string> validProfileExtensions = new List<string> { ".jpeg", ".png", ".jpg" };
            List<string> validDocumentExtensions = new List<string> { ".pdf" };

            if (admin != null && ModelState.IsValid)
            {

                try
                {
                    Guid generatedId = Guid.NewGuid();

                    Aspnetuser aspUser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = model.UserName,
                        Passwordhash = AuthHelper.GenerateSHA256(model.Password),
                        Email = model.Email,
                        Phonenumber = model.Phone,
                        Createddate = DateTime.Now,
                        Roleid = (int)AllowRole.Physician,
                    };

                    _unitOfWork.AspNetUserRepository.Add(aspUser);
                    _unitOfWork.Save();

                    Physician phy = new()
                    {
                        Aspnetuserid = generatedId.ToString(),
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Email = model.Email,
                        Mobile = model.Phone,
                        Medicallicense = model.MedicalLicenseNumber,
                        Adminnotes = model.AdminNotes,
                        Address1 = model.Address1,
                        Address2 = model.Address2,
                        City = model.City,
                        Regionid = model.RegionId,
                        Zip = model.Zip,
                        Altphone = model.MailPhone,
                        Createdby = admin.Aspnetuserid,
                        Createddate = DateTime.Now,
                        Status = (short)model.StatusId,
                        Roleid = model.RoleId,
                        Npinumber = model.NPINumber,
                        Businessname = model.BusinessName,
                        Businesswebsite = model.BusinessWebsite,
                    };

                    _unitOfWork.PhysicianRepository.Add(phy);
                    _unitOfWork.Save();

                    foreach(int regionId in model.selectedRegions)
                    {
                        Physicianregion phyRegion = new Physicianregion()
                        {
                            Regionid = regionId,
                            Physicianid = phy.Physicianid,
                        };

                        _unitOfWork.PhysicianRegionRepo.Add(phyRegion);
                    }

                    _unitOfWork.Save();


                    string path = Path.Combine(_environment.WebRootPath, "document", "physician", phy.Physicianid.ToString());

                    if (model.PhotoFile != null)
                    {
                        string fileExtension = Path.GetExtension(model.PhotoFile.FileName);
                        if (validProfileExtensions.Contains(fileExtension))
                        {
                            phy.Isnondisclosuredoc = true;
                            InsertFileAfterRename(model.PhotoFile, path, "ProfilePhoto");
                        }
                    }

                    if (model.SignatureFile != null)
                    {
                        string fileExtension = Path.GetExtension(model.SignatureFile.FileName);
                        if (validProfileExtensions.Contains(fileExtension))
                        {
                            phy.Isnondisclosuredoc = true;
                            InsertFileAfterRename(model.SignatureFile, path, "Signature");
                        }
                    }


                    if (model.ICAFile != null)
                    {
                        string fileExtension = Path.GetExtension(model.ICAFile.FileName);
                        if (validDocumentExtensions.Contains(fileExtension))
                        {
                            phy.Isnondisclosuredoc = true;
                            InsertFileAfterRename(model.ICAFile, path, "ICA");
                        }
                    }

                    if (model.BGCheckFile != null)
                    {
                        string fileExtension = Path.GetExtension(model.BGCheckFile.FileName);
                        if (validDocumentExtensions.Contains(fileExtension))
                        {
                            phy.Isnondisclosuredoc = true;
                            InsertFileAfterRename(model.BGCheckFile, path, "BackgroundCheck");
                        }
                    }

                    if (model.HIPAAComplianceFile != null)
                    {
                        string fileExtension = Path.GetExtension(model.HIPAAComplianceFile.FileName);
                        if (validDocumentExtensions.Contains(fileExtension))
                        {
                            phy.Isnondisclosuredoc = true;
                            InsertFileAfterRename(model.HIPAAComplianceFile, path, "HipaaCompliance");
                        }
                    }

                    if (model.NDAFile != null)
                    {
                        string fileExtension = Path.GetExtension(model.NDAFile.FileName);
                        if (validDocumentExtensions.Contains(fileExtension))
                        {
                            phy.Isnondisclosuredoc = true;
                            InsertFileAfterRename(model.NDAFile, path, "NDA");
                        }
                    }

                    if (model.LicenseDocFile != null)
                    {
                        string fileExtension = Path.GetExtension(model.LicenseDocFile.FileName);
                        if (validDocumentExtensions.Contains(fileExtension))
                        {
                            phy.Isnondisclosuredoc = true;
                            InsertFileAfterRename(model.LicenseDocFile, path, "LicenseDoc");
                        }
                    }

                    _unitOfWork.PhysicianRepository.Update(phy);
                    _unitOfWork.Save();

                    TempData["success"] = "Physician Created Successfully";

                    return RedirectToAction("ProviderMenu");
                }
                catch (Exception e)
                {
                    TempData["error"] = e.Message;
                    model.roles = _unitOfWork.RoleRepo.GetAll();
                    model.regions = _unitOfWork.RegionRepository.GetAll();
                    return View("Providers/CreatePhysicianAccount", model);
                }


            }

            model.roles = _unitOfWork.RoleRepo.GetAll();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Providers/CreatePhysicianAccount", model);

        }

        public IActionResult EditPhysicianAccount(int physicianId)
        {
            Physician physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == physicianId);
            IEnumerable<int> phyRegions = _unitOfWork.PhysicianRegionRepo.Where(pr => pr.Physicianid == physicianId).ToList().Select(_ => (int)_.Regionid); ;

            EditPhysicianViewModel model = new EditPhysicianViewModel()
            {
                FirstName = physician.Firstname,
                LastName = physician.Lastname,
                Email = physician.Email,
                Phone = physician.Mobile,
                MedicalLicenseNumber = physician.Medicallicense,
                NPINumber = physician.Npinumber,
                SyncEmail = physician.Syncemailaddress,
                Address1 = physician.Address1,
                Address2 = physician.Address2,
                City = physician.City,
                RegionId = physician.Regionid,
                Zip = physician.Zip,
                RoleId = (int)physician.Roleid,
                MailPhone = physician.Altphone,
                BusinessName = physician.Businessname,
                BusinessWebsite = physician.Businesswebsite,
                regions = _unitOfWork.RegionRepository.GetAll(),
                roles = _unitOfWork.RoleRepo.GetAll(),
                physicianRegions = phyRegions,
                IsICA = physician.Isagreementdoc ?? false,
                IsBGCheck = physician.Isbackgrounddoc ?? false,
                IsHIPAA = physician.Iscredentialdoc ?? false,
                IsLicenseDoc = physician.Islicensedoc ?? false,
                IsNDA = physician.Isnondisclosuredoc ?? false,
            };

            return View("Providers/EditPhysicianAccount", model);
        }

        public void StopNotification(int physicianId)
        {
            Physiciannotification notif = _context.Physiciannotifications.FirstOrDefault(x => x.Physicianid == physicianId);

            if (notif != null)
            {

                notif.Isnotificationstopped = !notif.Isnotificationstopped;

                _context.Physiciannotifications.Update(notif);
                _context.SaveChanges();
                return;

            }
            else
            {
                Physiciannotification obj = new()
                {
                    Physicianid = physicianId,
                    Isnotificationstopped = true
                };
                _context.Physiciannotifications.Add(obj);
                _context.SaveChanges();
                return;
            }
        }

        public IActionResult ProviderMenu()
        {

            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            IEnumerable<ProviderMenuRow> physicianList = (from phy in _context.Physicians
                                                          join role in _context.Roles on phy.Roleid equals role.Roleid
                                                          join pn in _context.Physiciannotifications on phy.Physicianid equals pn.Physicianid into notiGroup
                                                          from notiItem in notiGroup.DefaultIfEmpty()
                                                          select new ProviderMenuRow
                                                          {
                                                              PhysicianId = phy.Physicianid,
                                                              PhysicianName = phy.Firstname + " " + phy.Lastname,
                                                              Email = phy.Email,
                                                              PhoneNumber = phy.Mobile ?? "Mobile",
                                                              Role = role.Name,
                                                              Status = phy.Status.ToString() ?? "Status",
                                                              OnCallStatus = "Busy",
                                                              IsNotificationStopped = notiItem.Isnotificationstopped ? true : false,
                                                          }).OrderBy(_ => _.PhysicianId);

            ProviderMenuViewModel model = new ProviderMenuViewModel()
            {
                UserName = adminName,
                physicianList = physicianList,
            };
            return View("Providers/ProviderMenu", model);
        }


        public IActionResult ContactYourProviderModal(int physicianId)
        {
            ContactYourProviderModel model = new ContactYourProviderModel()
            {
                PhysicianId = physicianId,
            };
            return PartialView("Modals/ContactYourProvider", model);
        }

        [HttpPost]
        public IActionResult ContactYourProviderModal(ContactYourProviderModel model)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    Physician physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == model.PhysicianId);
                    if (model.CommunicationType == 2 || model.CommunicationType == 3)
                    {
                        string subject = "Contacting Provider";
                        string body = "<h2>Admin Message</h2><h5>" + model.Message + "</h5>";
                        string toEmail = physician.Email;
                        _emailService.SendMail(toEmail, body, subject);
                    }
                }

                TempData["success"] = "Messages sent successfully.";

            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return Redirect("/Admin/ProviderMenu");
        }

        public IActionResult Invoicing()
        {
            return View("Providers/Invoicing");
        }

        #endregion

        #region Profile


        [HttpPost]
        public bool SaveAdminAccountInfo(string password)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);

            if (admin == null)
            {
                TempData["error"] = "Admin not found";
                return false;
            }

            Aspnetuser aspUser = _unitOfWork.AspNetUserRepository.GetFirstOrDefault(asp => asp.Id == admin.Aspnetuserid);
            if (aspUser == null)
            {
                TempData["error"] = "Asp User not found";
                return false;
            }

            try
            {

                string passHash = AuthHelper.GenerateSHA256(password);

                aspUser.Passwordhash = passHash;
                _unitOfWork.AspNetUserRepository.Update(aspUser);
                _unitOfWork.Save();

                TempData["success"] = "Password Reset Successfully";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return false;
            }

        }

        [HttpPost]
        public IEnumerable<City> GetCitiesByRegion(int regionId)
        {
            return _utilityService.GetCitiesByRegion(regionId);
        }

        [HttpPost]
        public bool SaveAdministratorInfo(List<int> regions, string firstName, string lastName, string email, string phone)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);

            if (admin == null)
            {
                TempData["error"] = "Admin not found";
                return false;
            }

            try
            {
                admin.Firstname = firstName;
                admin.Lastname = lastName;
                admin.Mobile = phone;

                _unitOfWork.AdminRepository.Update(admin);

                _unitOfWork.Save();

                List<int> adminRegions = _unitOfWork.AdminRegionRepo.Where(region => region.Adminid == adminId).ToList().Select(x => (int)x.Regionid).ToList();

                List<int> commonRegions = new List<int>();

                // Finding common regions in both new and old lists
                foreach (int region in adminRegions)
                {
                    if (regions.Contains(region))
                    {
                        commonRegions.Add(region);
                    }
                }

                // Removing them from both lists
                foreach (int region in commonRegions)
                {
                    adminRegions.Remove(region);
                    regions.Remove(region);
                }

                // From difference we will remove regions that were in old list but not in new list
                foreach (int region in adminRegions)
                {
                    Adminregion ar = _unitOfWork.AdminRegionRepo.GetFirstOrDefault(ar => ar.Regionid == region);
                    _unitOfWork.AdminRegionRepo.Remove(ar);
                }

                // And Add the regions that were in new list but not in old list
                foreach (int region in regions)
                {
                    Adminregion adminregion = new Adminregion()
                    {
                        Adminid = adminId,
                        Regionid = region,
                    };

                    _unitOfWork.AdminRegionRepo.Add(adminregion);
                }

                _unitOfWork.Save();

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }

        }

        [HttpPost]
        public bool SaveAdminBillingInfo(string Address1, string Address2, int CityId, string Zip, string AltCountryCode, string AltPhoneNumber, int RegionId)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);
            string? City = _context.Cities.FirstOrDefault(city=> city.Id == CityId)?.Name;

            if (admin == null)
            {
                TempData["error"] = "Admin not found";
                return false;
            }

            try
            {

                string phone = "+" + AltCountryCode + "-" + AltPhoneNumber;
                admin.Address1 = Address1;
                admin.Address2 = Address2;
                admin.City = City;
                admin.Regionid = RegionId;
                admin.Altphone = phone;
                admin.Zip = Zip;

                _unitOfWork.AdminRepository.Update(admin);
                _unitOfWork.Save();

                return true;
            }
            catch (Exception e)
            {
                return false;

            }

        }



        #endregion

        #region Access


        [HttpGet]
        public IActionResult CreateAdminAccount()
        {
            EditPhysicianViewModel model = new EditPhysicianViewModel();
            model.roles = _context.Roles.ToList();
            model.regions = _context.Regions.ToList();
            return View("Access/CreateAdminAccount", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAdminAccount(EditPhysicianViewModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin? admin = _context.Admins.FirstOrDefault(x => x.Adminid == adminId);

            if (admin != null && ModelState.IsValid)
            {
                Guid generatedId = Guid.NewGuid();
                var phoneNumber = "+" + model.CountryCode + "-" + model.Phone;
                Aspnetuser aspnetuser = new()
                {
                    Id = generatedId.ToString(),
                    Username = model.UserName,
                    Passwordhash = AuthHelper.GenerateSHA256(model.Password),
                    Email = model.Email,
                    Phonenumber = model.Phone,
                    Createddate = DateTime.Now,
                    Roleid = 1,
                };

                _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                _unitOfWork.Save();

                Admin admin1 = new()
                {
                    Aspnetuserid = aspnetuser.Id,
                    Firstname = model.FirstName,
                    Lastname = model.LastName,
                    Email = model.Email,
                    Mobile = model.Phone,
                    Address1 = model.Address1,
                    Address2 = model.Address2,
                    City = model.City,
                    Regionid = model.RegionId,
                    Zip = model.Zip,
                    Createdby = aspnetuser.Id,
                    Createddate = DateTime.Now,
                    Roleid = model.RoleId,
                };
                _unitOfWork.AdminRepository.Add(admin1);
                _unitOfWork.Save();


                if (model.selectedRegions != null && model.selectedRegions.Any())
                {
                    foreach (int Item in model.selectedRegions)
                    {
                        Adminregion adminregion = new()
                        {
                            Regionid = Item,
                            Adminid = admin1.Adminid
                        };
                        _context.Adminregions.Add(adminregion);
                        _context.SaveChanges();
                    }
                }


                TempData["success"] = "Admin Created sucessfully";
                return RedirectToAction("UserAccess");
            }
            TempData["failure"] = "Error Creating Admin ";
            return RedirectToAction("CreateAdminAccount");
        }


        public IActionResult AccountAccess()
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers
                .Where(x => x.Key == "userId")
                .FirstOrDefault().Value);

            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);
            AccountAccessViewModel model = new AccountAccessViewModel();

            IEnumerable<AccountAccessTRow> accessTables = (from r in _context.Roles
                                                           where r.Isdeleted != true
                                                           select new AccountAccessTRow
                                                           {
                                                               Id = r.Roleid,
                                                               Name = r.Name,
                                                               AccounttypeName = r.Accounttype == 1 ? "Admin" : "Physician",
                                                               AccountType = r.Accounttype,
                                                           });

            model.roles = accessTables;
            return View("Access/AccountAccess", model);
        }

        public IActionResult EditRole(int roleid, int accounttype)
        {

            IEnumerable<int?> list1 = _context.Rolemenus.Where(a => a.Roleid == roleid).ToList().Select(x => x.Menuid);

            List<Menu> menus;

            if (accounttype == 3)
            {
                menus = _context.Menus.ToList();
            }
            else
            {
                menus = _context.Menus.Where(x => x.Accounttype == accounttype).ToList();
            }

            EditRoleViewModel model = new()
            {
                Menus = menus,
                list = list1,
                RoleId = roleid
            };

            return View("Access/EditRole", model);
        }

        [HttpPost]
        public bool roleEditSubmit(List<int> menus, int roleid)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            Admin? admin = _context.Admins.FirstOrDefault(a => a.Adminid == adminId);
            if (admin == null)
            {
                return false;
            }
            if (roleid == 0 || menus.Count == 0)
            {
                return false;

            }

            List<Rolemenu> rolemenus1 = _context.Rolemenus.ToList().Where(x => x.Roleid == roleid).ToList();
            List<int?> rolemenus = _context.Rolemenus.Where(x => x.Roleid == roleid).Select(x => x.Menuid).ToList();
            for (int i = 0; i < rolemenus.Count; i++)
            {
                if (!menus.Contains((int)rolemenus[i]))
                {
                    Role role = _context.Roles.FirstOrDefault(r => r.Roleid == roleid);
                    role.Modifiedby = admin.Aspnetuserid;
                    role.Modifieddate = DateTime.Now;
                    Rolemenu? rolemenu = _context.Rolemenus.FirstOrDefault(x => x.Menuid == rolemenus[i]);
                    _context.Rolemenus.Remove(rolemenu);
                    _context.Roles.Update(role);
                    _context.SaveChanges();
                }
            }
            for (int i = 0; i < menus.Count; i++)
            {
                if (!rolemenus.Contains(menus[i]))
                {

                    Role role = _context.Roles.FirstOrDefault(r => r.Roleid == roleid);
                    role.Modifiedby = admin.Aspnetuserid;
                    role.Modifieddate = DateTime.Now;
                    Rolemenu item = new Rolemenu();
                    item.Menuid = menus[i];
                    item.Roleid = roleid;
                    _context.Rolemenus.Add(item);
                    _context.Roles.Update(role);
                    _context.SaveChanges();

                }
                else
                {
                    continue;
                }
            }

            return true;

        }
        public IActionResult DeleteRole(int roleid)
        {
            try
            {

                int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
                Admin? admin = _context.Admins.FirstOrDefault(x => x.Adminid == adminId);

                Role? role = _context.Roles.FirstOrDefault(z => z.Roleid == roleid);

                if (role == null)
                {
                    TempData["error"] = "Error occured while removing role. Please try again later.";
                    return RedirectToAction("AccountAccess");
                }

                role.Isdeleted = true;
                _context.Roles.Update(role);
                _context.SaveChanges();

                TempData["success"] = "Role Deleted Successfully";

                return RedirectToAction("AccountAccess");
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
                return RedirectToAction("AccountAccess");
            }

        }

        [HttpGet]
        public ActionResult GetMenusByAccounttype(short type)
        {
            List<Menu> checkboxItems;
            checkboxItems = _context.Menus.Where(x => x.Accounttype == type).ToList();

            return Ok(checkboxItems);

        }

        [HttpGet]
        public IActionResult CreateAccess(short menuFilter)
        {
            List<Aspnetrole> roles = _context.Aspnetroles.ToList();
            CreateAccessViewModel model = new CreateAccessViewModel();
            model.netRoles = roles;
            return View("Access/CreateAccess", model);
        }

        [HttpPost]
        public IActionResult CreateAccess(CreateAccessViewModel model)
        {
            try
            {
                int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
                Admin? admin = _context.Admins.FirstOrDefault(x => x.Adminid == adminId);
                if (string.IsNullOrEmpty(model.roleName) || model.accounttype == 0 || model.selectedRoles == null || !model.selectedRoles.Any())
                {
                    TempData["error"] = "Please fill all necessary details";
                    return RedirectToAction("CreateAccess");
                }
                Role role = new()
                {
                    Name = model.roleName,
                    Accounttype = (short)model.accounttype,
                    Createdby = admin.Aspnetuserid,
                    Createddate = DateTime.Now,
                    Isdeleted = false

                };

                _unitOfWork.RoleRepo.Add(role);
                _unitOfWork.Save();

                foreach (var menuId in model.selectedRoles)
                {
                    Rolemenu rolemenu = new Rolemenu();
                    rolemenu.Menuid = menuId;
                    rolemenu.Roleid = role.Roleid;
                    _context.Rolemenus.Add(rolemenu);
                }

                _context.SaveChanges();

                TempData["success"] = "Role created Successfully";
                return RedirectToAction("AccountAccess");

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("CreateAccess");
            }

        }

        public IActionResult UserAccess()
        {
            IEnumerable<UserAccessTRow> list = (from user in _context.Aspnetusers
                                                select new UserAccessTRow
                                                {
                                                    AccountTypeId = user.Roleid,
                                                    AspnetUserId = user.Id,
                                                    AccountType = user.Username,
                                                    AccountPOC = user.Roleid.ToString(),
                                                    Phone = user.Phonenumber ?? "+91 XX XX XX XX XX",
                                                    Status = "Offline",
                                                    OpenRequests = "0",
                                                });
            UserAccessViewModel model = new()
            {
                userList = list,
            };
            return View("Access/UserAccess", model);
        }
        #endregion

        #region Records

        [HttpPost]
        public IActionResult SMSLogsPartialTable(int roleIdFilter, string receiverName, string mobileNumber, DateTime createdDate, DateTime sentDate)
        {
            IEnumerable<Smslog> logs = _context.Smslogs;

            SMSLogsViewModel model = new()
            {
                smsLogs = logs,
            };
            return PartialView("Partial/SMSLogPartialTable", model);
        }

        [HttpPost]
        public IActionResult EmailLogsPartialTable(int roleIdFilter, string receiverName, string emailAddress, DateTime createdDate, DateTime sentDate)
        {
            IEnumerable<Emaillog> logs = _context.Emaillogs;

            EmailLogsViewModel model = new()
            {
                emailLogs = logs,
            };
            return PartialView("Partial/EmailLogPartialTable",model);
        }

        [HttpPost]
        public IActionResult SearchRecordPartialTable(string patientName, int requestStatus, int requestType,string phoneNumber, DateTime fromDateOfService, DateTime toDateOfService,string providerName, string email)
        {

            // TODO: Add dateOfService filter in page
            var query = (from r in _context.Requests
                         join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                         join phy in _context.Physicians on r.Physicianid equals phy.Physicianid into phyGroup
                         from phyItem in phyGroup.DefaultIfEmpty()
                         where ( (string.IsNullOrEmpty(patientName) || (rc.Firstname+ " " + rc.Lastname).ToLower().Contains(patientName.ToLower()))
                         && (requestStatus == 0 || r.Status == requestStatus)
                         && (requestType == 0 || r.Requesttypeid == requestType)
                         && (string.IsNullOrEmpty(phoneNumber) || rc.Phonenumber.ToLower().Contains(phoneNumber.ToLower()))
                         && (string.IsNullOrEmpty(providerName) || (phyItem.Firstname + " " + phyItem.Lastname).ToLower().Contains(providerName.ToLower()))
                         && (string.IsNullOrEmpty(email) || rc.Email.ToLower().Contains(email.ToLower()))
                         )
                         select new SearchRecordTRow
                         {
                             RequestId = r.Requestid,
                             PatientName = rc.Firstname + " " + rc.Lastname,
                             Requestor = GetRequestType(r.Requesttypeid),
                             DateOfService = DateTime.Now,
                             CloseCaseDate = DateTime.Now,
                             Email = rc.Email ?? "",
                             PhoneNumber = rc.Phonenumber ?? "",
                             Address = rc.Address ?? "",
                             Zip = rc.Zipcode ?? "",
                             RequestStatus = RequestHelper.GetRequestStatusString(r.Status),
                             Physician = phyItem.Firstname + " " + phyItem.Lastname,
                             PhysicianNote = "",
                             AdminNote = "",
                             CancelledByPhysicianNote = "",
                             PatientNote = "",                             
                         });

            SearchRecordViewModel model = new SearchRecordViewModel()
            {
                searchRecordTRows = query
            };

            return PartialView("Partial/SearchRecordPartialTable",model);
        }


        public IActionResult SearchRecords()
        {
            SearchRecordViewModel model = new SearchRecordViewModel()
            {
                requeststatuses = _context.Requeststatuses,
                requesttypes = _context.Requesttypes,
            };
            return View("Records/SearchRecords",model);
        }

        public IActionResult EmailLogs()
        {

            EmailLogsViewModel model = new()
            {
                roles = _unitOfWork.RoleRepo.GetAll(),
            };
            return View("Records/EmailLogs",model);
        }

        public IActionResult SMSLogs()
        {
            SMSLogsViewModel model = new()
            {
                roles = _unitOfWork.RoleRepo.GetAll(),
            };
            return View("Records/SMSLogs",model);
        }

        public IActionResult PatientRecords()
        {
            return View("Records/PatientRecords");
        }

        public IActionResult BlockedHistory()
        {
            return View("Records/BlockedHistory");
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



        public void SendMailForCreateAccount(string email)
        {
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

                _emailService.SendMail(email, body, subject);

                TempData["success"] = "Email has been successfully sent to " + email + " for create account link.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
        }


        public void InsertRequestWiseFile(IFormFile document)
        {
            string path = _environment.WebRootPath + "/document";
            string fileName = document.FileName;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fullPath = Path.Combine(path, fileName);

            using FileStream stream = new(fullPath, FileMode.Create);
            document.CopyTo(stream);
        }


        public static string GetPatientDOB(Requestclient u)
        {
            string udb = u.Intyear + "-" + u.Strmonth + "-" + u.Intdate;
            if (u.Intyear == null || u.Strmonth == null || u.Intdate == null)
            {
                return "";
            }

            DateTime dobDate = DateTime.Parse(udb);
            string dob = dobDate.ToString("MMM dd, yyyy");
            var today = DateTime.Today;
            var age = today.Year - dobDate.Year;
            if (dobDate.Date > today.AddYears(-age)) age--;

            string dobString = dob + " (" + age + ")";

            return dobString;
        }

        public static string GetRequestType(int requestTypeId)
        {
            switch (requestTypeId)
            {
                case (int)RequestType.Business: return "Business";
                case (int)RequestType.Patient: return "Patient";
                case (int)RequestType.Concierge: return "Concierge";
                case (int)RequestType.Family: return "Relative/Family";
            }

            return null;
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
