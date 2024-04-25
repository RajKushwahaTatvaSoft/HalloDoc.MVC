using AspNetCoreHero.ToastNotification.Abstractions;
using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Utilities;
using ClosedXML.Excel;
using CsvHelper;
using Data_Layer.CustomModels;
using Data_Layer.CustomModels.Filter;
using Data_Layer.CustomModels.TableRow;
using Data_Layer.CustomModels.TableRow.Admin;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using HalloDoc.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.CodeAnalysis;
using Rotativa.AspNetCore;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Nodes;

namespace HalloDoc.MVC.Controllers
{

    [CustomAuthorize((int)AccountType.Admin)]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IUtilityService _utilityService;
        private readonly INotyfService _notyf;
        private readonly IAdminService _adminService;
        private readonly IJwtService _jwtService;

        public AdminController(IUnitOfWork unitOfWork, IJwtService jwtService, IAdminService adminService, IWebHostEnvironment environment, IConfiguration config, IEmailService emailService, IUtilityService utilityService, INotyfService notyf)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
            _config = config;
            _emailService = emailService;
            _utilityService = utilityService;
            _notyf = notyf;
            _adminService = adminService;
            _jwtService = jwtService;
        }

        #region Header

        [RoleAuthorize((int)AllowMenu.AdminProfile)]
        public IActionResult Profile()
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            try
            {

                AdminProfileViewModel? model = _adminService.AdminProfileService.GetAdminProfileModel(adminId);
                if (model == null)
                {
                    _notyf.Error("Cannot fetch data.");
                    return RedirectToAction("Dashboard");
                }

                model.UserName = adminName;
                return View("Header/Profile", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [RoleAuthorize((int)AllowMenu.ProviderLocation)]
        public IActionResult ProviderLocation()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {

                ProviderLocationViewModel? model = _adminService.ProviderLocationService.GetProviderLocationModel();

                if (model == null)
                {
                    return RedirectToAction("Dashboard");
                }

                model.UserName = adminName;

                return View("Header/ProviderLocation", model);

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


            TempData["success"] = "Logout Successfull";

            return Redirect("/Guest/Login");
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
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

                PagedList<AdminRequest> pagedList = await _adminService.AdminDashboardService.GetAdminRequestsAsync(filter);

                AdminDashboardViewModel model = new AdminDashboardViewModel();
                model.pagedList = pagedList;
                model.DashboardStatus = status;
                model.CurrentPage = pageNumber;
                model.filterOptions = filter;

                return PartialView("Partial/PartialTable", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Index", "Guest");
            }
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult Dashboard()
        {

            try
            {
                int? status = HttpContext.Session.GetInt32("currentStatus");
                int? page = HttpContext.Session.GetInt32("currentPage");
                int? region = HttpContext.Session.GetInt32("currentRegionFilter");
                int? type = HttpContext.Session.GetInt32("currentTypeFilter");
                string? search = HttpContext.Session.GetString("currentSearchFilter");

                DashboardFilter initialFilter = new DashboardFilter();
                initialFilter.Status = status ?? 1;
                initialFilter.PageNumber = page ?? 1;
                initialFilter.RegionFilter = region ?? 0;
                initialFilter.RequestTypeFilter = type ?? 0;
                initialFilter.PatientSearchText = search ?? "";


                AdminDashboardViewModel model = new AdminDashboardViewModel();

                model.physicians = _unitOfWork.PhysicianRepository.GetAll();
                model.regions = _unitOfWork.RegionRepository.GetAll();
                model.NewReqCount = _unitOfWork.RequestRepository.Where(r => r.Status == (short)RequestStatus.Unassigned).Count();
                model.PendingReqCount = _unitOfWork.RequestRepository.Where(r => r.Status == (short)RequestStatus.Accepted).Count();
                model.ActiveReqCount = _unitOfWork.RequestRepository.Where(r => (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite)).Count();
                model.ConcludeReqCount = _unitOfWork.RequestRepository.Where(r => r.Status == (short)RequestStatus.Conclude).Count();
                model.ToCloseReqCount = _unitOfWork.RequestRepository.Where(r => (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed)).Count();
                model.UnpaidReqCount = _unitOfWork.RequestRepository.Where(r => r.Status == (short)RequestStatus.Unpaid).Count();
                model.casetags = _unitOfWork.CaseTagRepository.GetAll();
                model.filterOptions = initialFilter;

                return View("Header/Dashboard", model);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Index", "Guest");
            }

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

                Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == model.RequestId);
                if (req == null)
                {
                    _notyf.Error("Cannot find request. Please try again later.");
                    return View("Error");
                }

                req.Modifieddate = currentTime;
                req.Physicianid = model.PhysicianId;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Physician phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == model.PhysicianId);
                string logNotes = adminName + " assigned to " + phy.Firstname + " " + phy.Lastname + " on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss") + " : " + model.Notes;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = model.RequestId ?? 0,
                    Status = (short)RequestStatus.Unassigned,
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
        public IActionResult TransferCaseModal(int requestId, int physicianId)
        {
            AssignCaseModel model = new AssignCaseModel()
            {
                RequestId = requestId,
                PhysicianId = physicianId,
                regions = _unitOfWork.RegionRepository.GetAll(),
            };

            return PartialView("Modals/TransferCaseModal", model);

        }

        [HttpPost]
        public IActionResult TransferCaseModal(AssignCaseModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

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
                    Requestid = model.RequestId ?? 0,
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
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == model.RequestId);
                Requestclient? reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == model.RequestId);

                if (req == null || reqCli == null)
                {
                    TempData["error"] = NotificationMessage.REQUEST_NOT_FOUND;
                    return Redirect("/Admin/Dashboard");
                }

                DateTime currentTime = DateTime.Now;
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
                    Createddate = currentTime,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                Blockrequest? oldBlockRequest = _unitOfWork.BlockRequestRepo.GetFirstOrDefault(block => block.Requestid == model.RequestId);
                if (oldBlockRequest != null)
                {
                    oldBlockRequest.Isactive = true;
                    oldBlockRequest.Modifieddate = currentTime;
                    oldBlockRequest.Reason = model.Reason;
                    oldBlockRequest.Phonenumber = reqCli.Phonenumber;
                    oldBlockRequest.Email = reqCli.Email;

                    _unitOfWork.BlockRequestRepo.Update(oldBlockRequest);
                }
                else
                {
                    Blockrequest currentBlockrequest = new Blockrequest()
                    {
                        Phonenumber = reqCli.Phonenumber,
                        Email = reqCli.Email,
                        Reason = model.Reason,
                        Requestid = model.RequestId,
                        Createddate = DateTime.Now,
                        Isactive = true,
                    };

                    _unitOfWork.BlockRequestRepo.Add(currentBlockrequest);
                }

                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Blocked";
                return Redirect("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
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
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value;

            if (adminId != 0)
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
                string? agreementLink = Url.Action("ReviewAgreement", "Guest", new { requestId = encryptedId }, Request.Scheme);

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

        [HttpGet]
        public IActionResult SendLinkModal()
        {
            return PartialView("Modals/SendLinkModal");
        }

        [HttpPost]
        public bool SendLinkForSubmitRequest(SendLinkModel model)
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
                            Recipientname = model.FirstName + " " + model.LastName,
                            Emailtemplate = "1",
                            Subjectname = subject,
                            Emailid = model.Email,
                            Roleid = (int)AccountType.Patient,
                            Adminid = adminId,
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
                        Adminid = adminId,
                        Createdate = DateTime.Now,
                        Sentdate = DateTime.Now,
                        Issmssent = true,
                        Senttries = 1,
                    };

                    _unitOfWork.SMSLogRepository.Add(smsLog);
                    _unitOfWork.Save();

                    _notyf.Success("Link Send successfully.");
                    return true;
                }
                catch (Exception e)
                {
                    TempData["error"] = "Error occurred : " + e.Message;
                    return false;
                }
            }

            TempData["error"] = "Please Fill all details for sending link.";
            return false;
        }


        #endregion

        #region ExportingExcel

        public async Task<IActionResult> ExportFilteredData(int status, int typeFilter, string searchFilter, int regionFilter)
        {
            try
            {

                int? page = HttpContext.Session.GetInt32("currentPage");

                int pageNumber = page < 1 ? 1 : page ?? 1;

                DashboardFilter filter = new DashboardFilter()
                {
                    RequestTypeFilter = typeFilter,
                    PatientSearchText = searchFilter,
                    RegionFilter = regionFilter,
                    PageNumber = pageNumber,
                    PageSize = 5,
                    Status = status,
                };

                PagedList<AdminRequest> pagedList = await _adminService.AdminDashboardService.GetAdminRequestsAsync(filter);

                if (pagedList.Count() < 1)
                {
                    _notyf.Error("No Request Data For Downloading");
                    return RedirectToAction("Dashboard");
                }

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
                        return File(stream.ToArray(), "application/vnd.ms-excel", fileName);
                    }
                }

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
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

                foreach (AdminRequest request in requestList)
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

                foreach (AdminRequest request in requestList)
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

                foreach (AdminRequest request in requestList)
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

                foreach (AdminRequest request in requestList)
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

                foreach (AdminRequest request in requestList)
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

        public IActionResult ExportAllExcel(int status)
        {
            try
            {

                IEnumerable<AdminRequest> allRequest = _adminService.AdminDashboardService.GetAllRequestByStatus(status);

                if (!allRequest.Any())
                {
                    _notyf.Error("No Request Data For Downloading");
                    return RedirectToAction("Dashboard");
                }
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
                        return File(stream.ToArray(), "application/vnd.ms-excel", fileName);
                    }
                }

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public string ExportAll(int status)
        {
            IEnumerable<AdminRequest> allRequest = _adminService.AdminDashboardService.GetAllRequestByStatus(status);

            string path = Path.Combine(_environment.WebRootPath, "export", "sample.csv");

            using (StreamWriter writer = new StreamWriter(path))

            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(allRequest);
            }

            return "export/sample.csv";

        }

        #endregion


        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult CreateRequest()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            IEnumerable<Region> regions = _unitOfWork.RegionRepository.GetAll();
            AdminCreateRequestViewModel model = new AdminCreateRequestViewModel();
            model.regions = regions;
            model.IsAdmin = true;
            return View("AdminProvider/CreateRequest", model);
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRequest(AdminCreateRequestViewModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            string? adminAspId = HttpContext.Request.Headers.Where(a => a.Key == "userAspId").FirstOrDefault().Value;

            if (adminId == 0 || string.IsNullOrEmpty(adminAspId))
            {
                _notyf.Error("Couldn't get admin data.");
                return RedirectToAction("Dashboard");
            }

            try
            {

                if (ModelState.IsValid)
                {
                    string? createLink = Url.Action("CreateAccount", "Guest", null, Request.Scheme);

                    ServiceResponse response = _adminService.AdminProviderService.SubmitCreateRequest(model, adminAspId, createLink ?? "", true);

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

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult Orders(int requestId)
        {

            SendOrderViewModel model = new SendOrderViewModel();
            model.professionalTypeList = _unitOfWork.HealthProfessionalTypeRepo.GetAll();
            model.RequestId = requestId;
            model.IsAdmin = true;

            return View("AdminProvider/Orders", model);
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        [HttpPost]
        public IActionResult Orders(SendOrderViewModel orderViewModel)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? adminAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

            try
            {
                if (ModelState.IsValid)
                {

                    ServiceResponse response = _adminService.AdminProviderService.SubmitOrderDetails(orderViewModel, adminAspId);

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

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult RequestDTY(string message)
        {
            string? adminName = HttpContext.Request.Headers.Where(a => a.Key == "userName").FirstOrDefault().Value;
            if (adminName == null)
            {
                TempData["failure"] = "Admin not found!!!";
                return View("error");
            }
            Admin admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Firstname + " " + a.Lastname == adminName);
            //PrviderOnCallViewModel model = new PrviderOnCallViewModel();
            DateTime current = DateTime.Now;

            IEnumerable<Physician> onDutyQuery = from shiftDetail in _unitOfWork.ShiftDetailRepository.GetAll()
                                                 join physician in _unitOfWork.PhysicianRepository.GetAll() on shiftDetail.Shift.Physicianid equals physician.Physicianid
                                                 //join physicianRegion in _context.Physicianregions on physician.Physicianid equals physicianRegion.Physicianid
                                                 //where (regionFilter == 0 || physicianRegion.Regionid == regionFilter)
                                                 where shiftDetail.Shiftdate.Date == current.Date
                                                 && TimeOnly.FromDateTime(current) >= shiftDetail.Starttime
                                                 && TimeOnly.FromDateTime(current) <= shiftDetail.Endtime
                                                 && shiftDetail.Isdeleted != true
                                                 select physician;

            IEnumerable<Physician> onDuty = onDutyQuery.Distinct();
            IEnumerable<Physician> offDuty = _unitOfWork.PhysicianRepository.GetAll().Except(onDuty).ToList();

            string subject = "Need urgent help";
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

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "HalloDoc"),
                Subject = subject,
                IsBodyHtml = true,
                Body = message,
            };

            foreach (Physician phy in offDuty)
            {
                string email = phy.Email;
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

            _unitOfWork.Save();

            if (offDuty.Count() >= 1)
            {
                client.Send(mailMessage);
                TempData["success"] = "Email sent successfully to all the off duty physicians";
                return RedirectToAction("Dashboard");
            }

            TempData["error"] = "Nobody's free";

            return RedirectToAction("Dashboard");
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult ViewCase(int? Requestid)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            if (Requestid == null)
            {
                return View("Error");
            }

            ViewCaseViewModel? model = _adminService.AdminProviderService.GetViewCaseModel(Requestid ?? 0);

            if (model == null)
            {
                _notyf.Error("Cannot get data. Please try again later.");
                return RedirectToAction("Dashboard");
            }

            model.UserName = adminName;
            model.IsAdmin = true;

            return View("AdminProvider/ViewCase", model);

        }


        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult ViewNotes(int requestId)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            ViewNotesViewModel? model = _adminService.AdminProviderService.GetViewNotesModel(requestId);

            if (model == null)
            {
                _notyf.Error("Cannot get data. Please try again later.");
                return RedirectToAction("Dashboard");
            }

            model.UserName = adminName;
            model.IsAdmin = true;

            return View("AdminProvider/ViewNotes", model);
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        [HttpPost]
        public IActionResult ViewNotes(ViewNotesViewModel model)
        {
            try
            {
                string? adminAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

                if (string.IsNullOrEmpty(adminAspId))
                {
                    _notyf.Error("Cannot get user id. Please try again later.");
                    return RedirectToAction("Dashboard");
                }

                ServiceResponse response = _adminService.AdminProviderService.SubmitViewNotes(model, adminAspId, true);

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

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult ViewUploads(int requestId)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            ViewUploadsViewModel? model = _adminService.AdminProviderService.GetViewUploadsModel(requestId, true);

            if (model == null)
            {
                _notyf.Error("Cannot get data. Please try again later.");
                return RedirectToAction("Dashboard");
            }


            return View("AdminProvider/ViewUploads", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult ViewUploads(ViewUploadsViewModel uploadsVM)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (uploadsVM.File != null)
            {
                FileHelper.InsertFileForRequest(uploadsVM.File, _environment.WebRootPath, uploadsVM.RequestId);

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

            return RedirectToAction("ViewUploads", new { requestId = uploadsVM.RequestId });
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
                Requestwisefile? file = _unitOfWork.RequestWiseFileRepository.GetFirstOrDefault(reqFile => reqFile.Requestwisefileid == requestWiseFileId);

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

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult CloseCase(int requestid)
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            Requestclient? requestClient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(s => s.Requestid == requestid);
            IEnumerable<Requestwisefile> docData = _unitOfWork.RequestWiseFileRepository.Where(s => s.Requestid == requestid);
            string? confirmationNumber = _unitOfWork.RequestRepository.GetFirstOrDefault(s => s.Requestid == requestid)?.Confirmationnumber;

            if (requestClient == null || confirmationNumber == null)
            {
                _notyf.Error("Cannot find request");
                return RedirectToAction("Dashboard");
            }

            DateTime? dobDate = null;
            if (requestClient.Intyear != null || requestClient.Intdate != null || requestClient.Strmonth != null)
            {
                dobDate = DateHelper.GetDOBDateTime(requestClient.Intyear ?? 0, requestClient.Strmonth, requestClient.Intdate ?? 0);
            }

            CloseCaseViewModel closeCase = new CloseCaseViewModel
            {
                UserName = adminName,
                FirstName = requestClient.Firstname,
                LastName = requestClient.Lastname,
                Email = requestClient.Email,
                PhoneNumber = requestClient.Phonenumber,
                Dob = dobDate,
                Files = docData,
                requestid = requestid,
                confirmatioNumber = confirmationNumber,
            };

            return View("Dashboard/CloseCase", closeCase);

        }


        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        [HttpPost]
        public IActionResult CloseCase(CloseCaseViewModel closeCase, int id)
        {
            try
            {

                Requestclient reqclient = _unitOfWork.RequestClientRepository.GetFirstOrDefault(s => s.Requestid == id);

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
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Dashboard/CloseCase", closeCase);
            }
        }


        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult CloseInstance(int reqid)
        {
            try
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

                    _notyf.Success("Case Closed");
                    return RedirectToAction("Dashboard");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("CloseCase", new { requestid = reqid });
            }
        }

        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult EncounterForm(int requestId)
        {
            try
            {

                EncounterFormViewModel? model = _adminService.AdminProviderService.GetEncounterFormModel(requestId, true);

                if (model == null)
                {
                    _notyf.Error("Cannot fetch data");
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
        [RoleAuthorize((int)AllowMenu.AdminDashboard)]
        public IActionResult EncounterForm(EncounterFormViewModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            model.IsAdmin = true;
            try
            {

                if (ModelState.IsValid)
                {
                    ServiceResponse response = _adminService.AdminProviderService.SubmitEncounterForm(model, true, adminId);

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


        #endregion

        #region Providers

        #region ProviderModals


        public int GetOffSet(int currentDay, List<int> repeatDays)
        {
            int nextVal = repeatDays.SkipWhile(day => day <= currentDay).FirstOrDefault();
            int index = repeatDays.IndexOf(nextVal);

            if (index < 0) index = 0;
            return index;
        }

        [HttpGet]
        public IActionResult AddShiftModal()
        {
            AddShiftModel model = new AddShiftModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            return PartialView("Modals/AddShiftModal", model);
        }

        [HttpPost]
        public bool AddShift(AddShiftModel model)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            string? adminAspId = HttpContext.Request.Headers.Where(a => a.Key == "userAspId").FirstOrDefault().Value;

            if (adminAspId == null || adminId == 0)
            {
                _notyf.Error("Admin Not found");
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
                    Createdby = adminAspId,
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
                        Status = (short)ShiftStatus.Approved,
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

        private DateTime GetNextWeekday(DateTime startDate, int targetDayOfWeek)
        {
            int currentDayOfWeek = (int)startDate.DayOfWeek;
            int daysToAdd = targetDayOfWeek - currentDayOfWeek;

            if (daysToAdd <= 0) daysToAdd += 7; // If the target day is earlier in the week, move to next week

            return startDate.AddDays(daysToAdd);
        }


        [HttpPost]
        public bool DeleteShift(int shiftDetailId)
        {
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

            string? adminAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;
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
                    sd.Modifiedby = adminAspId;

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

        public IActionResult ViewShiftModal(int shiftDetailId, int? physicianId)
        {
            Shiftdetail? shiftdetail = _unitOfWork.ShiftDetailRepository.GetFirstOrDefault(shift => shift.Shiftdetailid == shiftDetailId);
            if (shiftdetail == null)
            {
                return View("Error");
            }

            if (physicianId == null)
            {
                Shift? shift = _unitOfWork.ShiftRepository.GetFirstOrDefault(shift => shift.Shiftid == shiftdetail.Shiftid);
                physicianId = shift?.Physicianid;
            }

            Physician? phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == physicianId);

            DateTime currentTime = DateTime.Now;

            ViewShiftModel model = new ViewShiftModel()
            {
                PhysicianName = phy?.Firstname + " " + phy?.Lastname,
                RegionName = _unitOfWork.RegionRepository.GetFirstOrDefault(reg => reg.Regionid == shiftdetail.Regionid)?.Name,
                ShiftDate = shiftdetail.Shiftdate,
                ShiftEndTime = shiftdetail.Endtime,
                ShiftStartTime = shiftdetail.Starttime,
            };

            return PartialView("Modals/ViewShiftModal", model);

        }

        #endregion


        [HttpPost]
        public bool ApproveShifts(List<int> shiftids)
        {
            try
            {

                IEnumerable<Shiftdetail> shiftDetailsToApprove = _unitOfWork.ShiftDetailRepository.Where(sd => shiftids.Contains(sd.Shiftdetailid)).ToList();

                foreach (Shiftdetail shiftDetail in shiftDetailsToApprove)
                {
                    shiftDetail.Status = (int)ShiftStatus.Approved;
                    _unitOfWork.ShiftDetailRepository.Update(shiftDetail);
                    _unitOfWork.Save();
                }

                _notyf.Success("Shifts approved successfully.");
                return true;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public bool DeleteShifts(List<int> shiftids)
        {
            try
            {

                IEnumerable<Shiftdetail> shiftDetailsToRemove = _unitOfWork.ShiftDetailRepository.Where(sd => shiftids.Contains(sd.Shiftdetailid)).ToList();
                foreach (Shiftdetail shiftDetail in shiftDetailsToRemove)
                {
                    shiftDetail.Isdeleted = true;
                    _unitOfWork.ShiftDetailRepository.Update(shiftDetail);
                    _unitOfWork.Save();
                }

                _notyf.Success("Shifts deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }


        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public IActionResult ProviderOnCall()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            ProviderOnCallViewModel model = new ProviderOnCallViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.LoggedInUserName = adminName;

            return View("Providers/ProviderOnCall", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public IActionResult POCpartial(int regionFilter)
        {
            try
            {
                ProviderOnCallViewModel model = new ProviderOnCallViewModel();
                DateTime current = DateTime.Now;

                IEnumerable<Physician> onDutyQuery = from shiftDetail in _unitOfWork.ShiftDetailRepository.GetAll()
                                                     join physician in _unitOfWork.PhysicianRepository.GetAll() on shiftDetail.Shift.Physicianid equals physician.Physicianid
                                                     join physicianRegion in _unitOfWork.PhysicianRegionRepo.GetAll() on physician.Physicianid equals physicianRegion.Physicianid
                                                     where (regionFilter == 0 || physicianRegion.Regionid == regionFilter)
                                                     && shiftDetail.Shiftdate.Date == current.Date
                                                     && TimeOnly.FromDateTime(current) >= shiftDetail.Starttime
                                                     && TimeOnly.FromDateTime(current) <= shiftDetail.Endtime
                                                     && shiftDetail.Isdeleted != true
                                                     select physician;

                IEnumerable<Physician> onDuty = onDutyQuery.Distinct();

                List<Physician> offDuty = _unitOfWork.PhysicianRepository.GetAll().AsEnumerable().Except(onDuty).ToList();

                model.physiciansOnCall = onDuty.ToList();
                model.physiciansOffDuty = offDuty.ToList();

                return PartialView("Providers/Partial/_ProviderOnCallPartial", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return PartialView("Providers/Partial/_ProviderOnCallPartial");
            }

        }


        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public IActionResult RequestedShift()
        {

            RequestShiftViewModel model = new RequestShiftViewModel()
            {
                regions = _unitOfWork.RegionRepository.GetAll(),
            };


            return View("Providers/RequestedShifts", model);

        }

        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public async Task<IActionResult> RequestShiftPartialTable(int pageNo, int regionFilter)
        {
            int pageNumber = pageNo;
            int pageSize = 10;

            IQueryable<RequestShiftTRow> list = (from shiftDetail in _unitOfWork.ShiftDetailRepository.GetAll()
                                                 where (regionFilter == 0 || shiftDetail.Regionid == regionFilter)
                                                 join region in _unitOfWork.RegionRepository.GetAll() on shiftDetail.Regionid equals region.Regionid
                                                 join shift in _unitOfWork.ShiftRepository.GetAll() on shiftDetail.Shiftid equals shift.Shiftid
                                                 join phy in _unitOfWork.PhysicianRepository.GetAll() on shift.Physicianid equals phy.Physicianid
                                                 select new RequestShiftTRow
                                                 {
                                                     ShiftDetailId = shiftDetail.Shiftdetailid,
                                                     ShiftDate = shiftDetail.Shiftdate,
                                                     ShiftStartTime = shiftDetail.Starttime,
                                                     ShiftEndTime = shiftDetail.Endtime,
                                                     Staff = phy.Firstname + " " + phy.Lastname,
                                                     RegionName = region.Name,
                                                 }).AsQueryable();


            PagedList<RequestShiftTRow> pagedList = await PagedList<RequestShiftTRow>.CreateAsync(
            list, pageNumber, pageSize);

            return PartialView("Providers/Partial/_RequestShiftPartialTable", pagedList);
        }


        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public IActionResult Scheduling(bool? isViewCurrentMonth)
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            SchedulingViewModel model = new SchedulingViewModel();
            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.UserName = adminName;
            model.IsViewMonth = isViewCurrentMonth ?? false;

            return View("Providers/Scheduling", model);
        }


        [HttpPost]
        public List<Physician> GetPhyByRegion(int id)
        {
            return _unitOfWork.PhysicianRepository.Where(a => a.Regionid == id).ToList();
        }


        [HttpPost]
        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public IActionResult ScheduleMonthWisePartial(int shiftMonth, int shiftYear, int regionFilter)
        {

            shiftMonth++;

            IEnumerable<Shiftdetail> query = _unitOfWork.ShiftDetailRepository.Where(shift => shift.Shiftdate.Month == shiftMonth && shift.Shiftdate.Year == shiftYear && (regionFilter == 0 || shift.Regionid == regionFilter));

            int days = DateTime.DaysInMonth(shiftYear, shiftMonth);
            DayOfWeek dayOfWeek = new DateTime(shiftYear, shiftMonth, 1).DayOfWeek;

            ShiftMonthViewModel model = new ShiftMonthViewModel()
            {
                StartDate = new DateTime(shiftYear, shiftMonth, 1),
                shiftDetails = query,
                DaysInMonth = days,
            };

            return PartialView("Providers/Partial/_ScheduleMonthWiseTable", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.Scheduling)]
        public IActionResult ScheduleDayWisePartial(DateTime dayDate, int regionFilter)
        {
            DateTime current = dayDate.ToLocalTime().Date;

            ShiftWeekViewModel model = new ShiftWeekViewModel();

            IEnumerable<Physician> phyList = _unitOfWork.PhysicianRepository.Where(phy => (regionFilter == 0 || phy.Regionid == regionFilter));
            List<PhysicianShift> physicianShifts = new List<PhysicianShift>();

            foreach (var phy in phyList)
            {
                var query = (from sd in _unitOfWork.ShiftDetailRepository.GetAll()
                             join s in _unitOfWork.ShiftRepository.GetAll() on sd.Shiftid equals s.Shiftid
                             where (sd.Isdeleted != true)
                             where (s.Physicianid == phy.Physicianid && sd.Shiftdate == current)
                             select sd);

                PhysicianShift shift = new()
                {
                    PhysicianId = phy.Physicianid,
                    PhysicianName = phy.Firstname,
                    shiftDetails = query,
                };

                physicianShifts.Add(shift);
            }

            model.physicianShifts = physicianShifts;

            return PartialView("Providers/Partial/_ScheduleDayWiseTable", model);

        }

        public static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        [HttpPost]
        public IActionResult ScheduleWeekWisePartial(DateTime startDate, int regionFilter)
        {

            DateTime start = startDate.ToLocalTime().Date;

            if (start.DayOfWeek != DayOfWeek.Sunday)
            {
                start = StartOfWeek(start, DayOfWeek.Sunday);
            }

            DateTime end = start.AddDays(7);

            IEnumerable<Physician> phyList = _unitOfWork.PhysicianRepository.Where(phy => (regionFilter == 0 || phy.Regionid == regionFilter));
            List<PhysicianShift> physicianShifts = new List<PhysicianShift>();

            foreach (var phy in phyList)
            {
                var query = (from sd in _unitOfWork.ShiftDetailRepository.GetAll()
                             join s in _unitOfWork.ShiftRepository.GetAll() on sd.Shiftid equals s.Shiftid
                             where (s.Physicianid == phy.Physicianid && sd.Shiftdate >= start && sd.Shiftdate <= end)
                             select sd);

                PhysicianShift shift = new()
                {
                    PhysicianId = phy.Physicianid,
                    PhysicianName = phy.Firstname,
                    shiftDetails = query,
                };

                physicianShifts.Add(shift);
            }

            ShiftWeekViewModel model = new ShiftWeekViewModel();
            model.StartOfWeek = start;
            model.physicianShifts = physicianShifts;

            return PartialView("Providers/Partial/_ScheduleWeekWiseTable", model);
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
        public bool UpdatePhysicianAccountInfo(int roleId, int statusId, int physicianId)
        {
            try
            {

                string? adminAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

                Role? role = _unitOfWork.RoleRepo.GetFirstOrDefault(role => role.Roleid == roleId);
                Physician? physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == physicianId);

                if (physician == null)
                {
                    _notyf.Error("Physician not found");
                    return false;
                }

                if (role == null || role.Accounttype != (int)AccountType.Physician)
                {
                    _notyf.Error("Please select valid role to update.");
                    return false;
                }

                physician.Roleid = roleId;
                physician.Status = (short)statusId;
                physician.Modifiedby = adminAspId;
                physician.Modifieddate = DateTime.Now;

                _unitOfWork.PhysicianRepository.Update(physician);
                _unitOfWork.Save();

                _notyf.Success("Details updated successfully.");
                return true;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }

        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
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

                    physician.Signature = "Signature" + sigExtension;
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
                    physician.Photo = "ProfilePhoto" + profileExtension;
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
        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
        public bool SavePhysicianOnboardingInfo(int PhysicianId, IFormFile ICAFile, IFormFile BGCheckFile, IFormFile HIPAAComplianceFile, IFormFile NDAFile, IFormFile LicenseDocFile)
        {

            if (PhysicianId == 0)
            {
                return false;
            }

            try
            {
                string path = Path.Combine(_environment.WebRootPath, "document", "physician", PhysicianId.ToString());
                Physician? phy = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == PhysicianId);

                if (phy == null)
                {
                    _notyf.Error("Physician not found");
                    return false;
                }

                if (ICAFile != null)
                {
                    InsertFileAfterRename(ICAFile, path, "ICA");
                    phy.Isagreementdoc = true;
                }


                if (BGCheckFile != null)
                {
                    InsertFileAfterRename(BGCheckFile, path, "BackgroundCheck");
                    phy.Isbackgrounddoc = true;
                }


                if (HIPAAComplianceFile != null)
                {
                    InsertFileAfterRename(HIPAAComplianceFile, path, "HipaaCompliance");
                    phy.Iscredentialdoc = true;
                }


                if (NDAFile != null)
                {
                    InsertFileAfterRename(NDAFile, path, "NDA");
                    phy.Isnondisclosuredoc = true;
                }


                if (LicenseDocFile != null)
                {
                    InsertFileAfterRename(LicenseDocFile, path, "LicenseDoc");
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

        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
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
        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
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

        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
        public IActionResult CreatePhysicianAccount()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            EditPhysicianViewModel model = new EditPhysicianViewModel()
            {
                regions = _unitOfWork.RegionRepository.GetAll(),
                roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Physician),
                LoggedInUserName = adminName,
            };
            return View("Providers/CreatePhysicianAccount", model);
        }

        public async Task<string> FetchLatLang(EditPhysicianViewModel model)
        {
            try
            {
                string city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.CityId).Name;
                string state = _unitOfWork.RegionRepository.GetFirstOrDefault(reg => reg.Regionid == model.RegionId).Name;

                using (var client = new HttpClient())
                {
                    string apiKey = _config.GetSection("Geocoding")["ApiKey"];
                    string baseUrl = $"https://geocode.maps.co/search?city={city}&state={state}&postalcode={model.Zip}&country=India&api_key=" + apiKey;
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


                return "sucess";
            }
            catch (Exception e)
            {
                var error = e.Message;
                return e.Message;
            }

        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
        public IActionResult CreatePhysicianAccount(EditPhysicianViewModel model)
        {
            try
            {
                int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
                string? adminAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

                model.roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Physician);
                model.regions = _unitOfWork.RegionRepository.GetAll();

                if (ModelState.IsValid)
                {
                    List<string> validProfileExtensions = new List<string> { ".jpeg", ".png", ".jpg" };
                    List<string> validDocumentExtensions = new List<string> { ".pdf" };

                    Physician? physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Email == model.Email);
                    if (physician != null)
                    {
                        _notyf.Error("Physician already exists with given email");
                        return View("Providers/CreatePhysicianAccount", model);
                    }

                    string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.CityId)?.Name;

                    try
                    {
                        Guid generatedId = Guid.NewGuid();

                        Aspnetuser aspUser = new()
                        {
                            Id = generatedId.ToString(),
                            Username = _utilityService.GenerateUserName(AccountType.Physician, model.FirstName, model.LastName),
                            Passwordhash = AuthHelper.GenerateSHA256(model.Password),
                            Email = model.Email,
                            Phonenumber = model.Phone,
                            Createddate = DateTime.Now,
                            Accounttypeid = (int)AccountType.Physician,
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
                            City = city,
                            Regionid = model.RegionId,
                            Zip = model.Zip,
                            Altphone = model.MailPhone,
                            Createdby = adminAspId,
                            Createddate = DateTime.Now,
                            Status = (short)AccountStatus.Active,
                            Roleid = model.RoleId,
                            Npinumber = model.NPINumber,
                            Businessname = model.BusinessName,
                            Businesswebsite = model.BusinessWebsite,
                        };

                        _unitOfWork.PhysicianRepository.Add(phy);
                        _unitOfWork.Save();

                        foreach (int regionId in model.selectedRegions)
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
                            string renameTo = "ProfilePhoto";
                            if (validProfileExtensions.Contains(fileExtension))
                            {
                                phy.Isnondisclosuredoc = true;
                                phy.Photo = renameTo + fileExtension;
                                InsertFileAfterRename(model.PhotoFile, path, renameTo);
                            }
                        }

                        if (model.SignatureFile != null)
                        {
                            string fileExtension = Path.GetExtension(model.SignatureFile.FileName);
                            string renameTo = "Signature";
                            if (validProfileExtensions.Contains(fileExtension))
                            {
                                phy.Isnondisclosuredoc = true;
                                phy.Signature = renameTo + fileExtension;
                                InsertFileAfterRename(model.SignatureFile, path, renameTo);
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
                        return View("Providers/CreatePhysicianAccount", model);
                    }

                }

                _notyf.Error("Please enter valid details");
                return View("Providers/CreatePhysicianAccount", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return View("Error");
            }

        }

        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
        public IActionResult EditPhysicianAccount(int physicianId, string? phyAspId)
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            Physician? physician;
            if (phyAspId != null)
            {
                physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Aspnetuserid == phyAspId);
            }
            else
            {
                physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == physicianId);
            }

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
                StatusId = physician.Status,
                LoggedInUserName = adminName,
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

            return View("Providers/EditPhysicianAccount", model);
        }

        public void StopNotification(int physicianId)
        {
            Physiciannotification notif = _unitOfWork.PhysicianNotificationRepo.GetFirstOrDefault(x => x.Physicianid == physicianId);

            if (notif != null)
            {

                notif.Isnotificationstopped = !notif.Isnotificationstopped;

                _unitOfWork.PhysicianNotificationRepo.Update(notif);
                _unitOfWork.Save();
                return;

            }
            else
            {
                Physiciannotification obj = new()
                {
                    Physicianid = physicianId,
                    Isnotificationstopped = true
                };
                _unitOfWork.PhysicianNotificationRepo.Add(obj);
                _unitOfWork.Save();
                return;
            }
        }

        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
        public async Task<IActionResult> ProviderMenuPartialTable(int pageNo, int regionFilter)
        {
            int pageNumber = pageNo;
            int pageSize = 5;

            DateTime current = DateTime.Now;
            IQueryable<int> onDutyQuery = from shiftDetail in _unitOfWork.ShiftDetailRepository.GetAll()
                                          join physician in _unitOfWork.PhysicianRepository.GetAll() on shiftDetail.Shift.Physicianid equals physician.Physicianid
                                          where shiftDetail.Shiftdate.Date == current.Date
                                          && TimeOnly.FromDateTime(current) >= shiftDetail.Starttime
                                          && TimeOnly.FromDateTime(current) <= shiftDetail.Endtime
                                          && shiftDetail.Isdeleted != true
                                          select physician.Physicianid;

            IEnumerable<int> onDutyPhysicianIds = onDutyQuery.Distinct();

            var physicianList = (from phy in _unitOfWork.PhysicianRepository.GetAll()
                                 join role in _unitOfWork.RoleRepo.GetAll() on phy.Roleid equals role.Roleid
                                 join pn in _unitOfWork.PhysicianNotificationRepo.GetAll() on phy.Physicianid equals pn.Physicianid into notiGroup
                                 from notiItem in notiGroup.DefaultIfEmpty()
                                 where (regionFilter == 0 || phy.Regionid == regionFilter)
                                 select new ProviderMenuTRow
                                 {
                                     PhysicianId = phy.Physicianid,
                                     PhysicianName = phy.Firstname + " " + phy.Lastname,
                                     Email = phy.Email,
                                     PhoneNumber = phy.Mobile ?? "Mobile",
                                     Role = role.Name,
                                     Status = phy.Status == (short)AccountStatus.Active ? "Active" : "Disabled",
                                     OnCallStatus = onDutyPhysicianIds.Contains(phy.Physicianid) ? "On Duty" : "Off Duty",
                                     IsNotificationStopped = notiItem.Isnotificationstopped ? true : false,
                                 });

            if (physicianList == null || !physicianList.Any())
            {
                return PartialView("Providers/Partial/_ProviderMenuPartialTable");
            }

            PagedList<ProviderMenuTRow> pagedList = await PagedList<ProviderMenuTRow>.CreateAsync(
            physicianList.AsQueryable(), pageNumber, pageSize);

            return PartialView("Providers/Partial/_ProviderMenuPartialTable", pagedList);
        }


        [RoleAuthorize((int)AllowMenu.ProviderMenu)]
        public IActionResult ProviderMenu()
        {

            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;
            ProviderMenuViewModel model = new ProviderMenuViewModel()
            {
                UserName = adminName,
                regions = _unitOfWork.RegionRepository.GetAll().OrderBy(_ => _.Name),
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
            // CommunicationType : 1 = SMS , 2 = MAIL , 3 = BOTH

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "userId").Value);
            try
            {
                if (ModelState.IsValid)
                {
                    Physician physician = _unitOfWork.PhysicianRepository.GetFirstOrDefault(phy => phy.Physicianid == model.PhysicianId);
                    if (model.CommunicationType == 3 || model.CommunicationType == 2)
                    {
                        string subject = "Contacting Provider";
                        string body = "<h2>Admin Message</h2><h5>" + model.Message + "</h5>";
                        string toEmail = physician.Email;

                        try
                        {
                            _emailService.SendMail(toEmail, body, subject, out int sentTries, out bool isSent);

                            Emaillog emailLog = new Emaillog()
                            {
                                Emailtemplate = "1",
                                Subjectname = subject,
                                Emailid = toEmail,
                                Roleid = (int)AccountType.Patient,
                                Adminid = adminId,
                                Createdate = DateTime.Now,
                                Sentdate = DateTime.Now,
                                Isemailsent = isSent,
                                Senttries = sentTries,
                            };

                            _unitOfWork.EmailLogRepository.Add(emailLog);
                            _unitOfWork.Save();

                            _notyf.Success("Email Sent Successfully");
                        }
                        catch (Exception e)
                        {
                            _notyf.Error("Cannot send email. Error : " + e.Message);
                        }

                    }

                    if (model.CommunicationType == 3 || model.CommunicationType == 1)
                    {
                        try
                        {
                            Smslog smsLog = new Smslog()
                            {
                                Smstemplate = "1",
                                Mobilenumber = physician.Mobile,
                                Roleid = (int)AccountType.Patient,
                                Adminid = adminId,
                                Createdate = DateTime.Now,
                                Sentdate = DateTime.Now,
                                Issmssent = true,
                                Senttries = 1,
                            };

                            _unitOfWork.SMSLogRepository.Add(smsLog);
                            _unitOfWork.Save();

                            _notyf.Success("SMS Sent Successfully");

                        }
                        catch (Exception e)
                        {
                            _notyf.Error("Cannot send SMS. Error : " + e.Message);
                        }

                    }

                }
                else
                {
                    _notyf.Error("Please Input All Required ");
                }


            }
            catch (Exception e)
            {
                _notyf.Error(e.Message);
            }
            return Redirect("/Admin/ProviderMenu");
        }


        [RoleAuthorize((int)AllowMenu.Invoicing)]
        public IActionResult Invoicing()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            return View("Providers/Invoicing", adminName);
        }

        #endregion

        #region Profile


        [HttpPost]
        [RoleAuthorize((int)AllowMenu.AdminProfile)]
        public bool AdminResetPassword(string password, int? adminId)
        {
            if (adminId == null)
            {
                adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            }

            try
            {
                ServiceResponse response = _adminService.AdminProfileService.UpdateAdminPassword(adminId ?? 0, password);

                if (response.StatusCode == ResponseCode.Success)
                {
                    _notyf.Success("Password Reset Successfully");
                    return true;
                }

                _notyf.Error(response.Message);
                return false;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }

        }

        [HttpPost]
        public IEnumerable<City> GetCitiesByRegion(int regionId)
        {
            return _utilityService.GetCitiesByRegion(regionId);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.AdminProfile)]
        public bool SaveAdminAccountInfo(int roleId, int statusId, int? adminId)
        {
            bool isProfile = false;
            try
            {

                if (adminId == null)
                {
                    isProfile = true;
                    adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
                }

                ServiceResponse response = _adminService.AdminProfileService.UpdateAdminAccountInfo(roleId, statusId, adminId ?? 0);

                if (response.StatusCode == ResponseCode.Success)
                {

                    if (!isProfile)
                    {
                        _notyf.Success("Admin updated Successfully.");
                        return true;
                    }

                    Response.Cookies.Delete("hallodoc");

                    SessionUser? sessionUser = _utilityService.GetSessionUserFromAdminId(adminId ?? 0);

                    if (sessionUser == null)
                    {
                        _notyf.Error("Cannot re-create session. Please login again.");
                        return false;
                    }

                    string jwtToken = _jwtService.GenerateJwtToken(sessionUser);

                    Response.Cookies.Append("hallodoc", jwtToken);

                    _notyf.Success("Profile updated Successfully.");
                    return true;
                }


                _notyf.Error(response.Message);
                return false;


            }
            catch (Exception e)
            {
                _notyf.Error(e.Message);
                return false;
            }
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.AdminProfile)]
        public bool SaveAdministratorInfo(List<int> regions, string firstName, string lastName, string email, string phone, int? adminId)
        {
            bool isProfile = false;

            try
            {

                if (adminId == null)
                {
                    isProfile = true;
                    adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
                }

                ServiceResponse response = _adminService.AdminProfileService.UpdateAdminPersonalDetails(adminId ?? 0, regions, firstName, lastName, email, phone);

                if (response.StatusCode == ResponseCode.Success)
                {

                    if (!isProfile)
                    {
                        _notyf.Success("Admin updated Successfully.");
                        return true;
                    }

                    Response.Cookies.Delete("hallodoc");

                    SessionUser? sessionUser = _utilityService.GetSessionUserFromAdminId(adminId ?? 0);

                    if (sessionUser == null)
                    {
                        _notyf.Error("Cannot re-create session. Please login again.");
                        return false;
                    }

                    string jwtToken = _jwtService.GenerateJwtToken(sessionUser);

                    Response.Cookies.Append("hallodoc", jwtToken);

                    _notyf.Success("Profile updated Successfully.");
                    return true;
                }

                _notyf.Error(response.Message);
                return false;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }

        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.AdminProfile)]
        public bool SaveAdminBillingInfo(int? adminId, string Address1, string Address2, int CityId, string Zip, string AltCountryCode, string AltPhoneNumber, int RegionId)
        {
            if (adminId == null)
            {
                adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            }
            Admin? admin = _unitOfWork.AdminRepository.GetFirstOrDefault(a => a.Adminid == adminId);

            if (admin == null)
            {
                TempData["error"] = "Admin not found";
                return false;
            }

            try
            {

                string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == CityId)?.Name;
                string phone = "+" + AltCountryCode + "-" + AltPhoneNumber;
                admin.Address1 = Address1;
                admin.Address2 = Address2;
                admin.City = city;
                admin.Regionid = RegionId;
                admin.Altphone = phone;
                admin.Zip = Zip;

                _unitOfWork.AdminRepository.Update(admin);
                _unitOfWork.Save();

                _notyf.Success("Details updated successfully.");
                return true;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;

            }

        }

        #endregion

        #region Access

        [RoleAuthorize((int)AllowMenu.UserAccess)]
        public IActionResult EditAdminAccount(string adminAspId)
        {
            string? loggedInAdminAspId = HttpContext.Request.Headers.Where(a => a.Key == "userAspId").FirstOrDefault().Value;
            if (loggedInAdminAspId == null)
            {
                _notyf.Error("Cannot find admin");
                return RedirectToAction("Dashboard");
            }

            if (loggedInAdminAspId.Equals(adminAspId))
            {
                return RedirectToAction("Profile");
            }

            try
            {

                AdminProfileViewModel? model = _adminService.AdminAccessService.GetEditAdminAccountModel(adminAspId);
                if (model == null)
                {
                    _notyf.Error("Cannot fetch data");
                    return RedirectToAction("Dashboard");
                }

                return View("Access/EditAdminAccount", model);
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("Dashboard");
            }

        }


        [HttpGet]
        [RoleAuthorize((int)AllowMenu.UserAccess)]
        public IActionResult CreateAdminAccount()
        {
            AdminProfileViewModel model = new AdminProfileViewModel();
            model.roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Admin);
            model.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Access/CreateAdminAccount", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize((int)AllowMenu.UserAccess)]
        public IActionResult CreateAdminAccount(AdminProfileViewModel model)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);

            if (ModelState.IsValid)
            {

                Admin? admin = _unitOfWork.AdminRepository.GetFirstOrDefault(admin => admin.Email == model.Email);
                if (admin != null)
                {
                    _notyf.Error("Physician already exists with given email");
                    return View("Access/CreateAdminAccount", model);
                }

                try
                {

                    string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.CityId)?.Name;
                    Guid generatedId = Guid.NewGuid();
                    string phoneNumber = "+" + model.CountryCode + "-" + model.PhoneNumber;
                    string altPhoneNumber = "+" + model.AltCountryCode + "-" + model.AltPhoneNumber;
                    Aspnetuser aspnetuser = new()
                    {
                        Id = generatedId.ToString(),
                        Username = _utilityService.GenerateUserName(AccountType.Admin, model.FirstName, model.LastName),
                        Passwordhash = AuthHelper.GenerateSHA256(model.Password),
                        Email = model.Email,
                        Phonenumber = phoneNumber,
                        Createddate = DateTime.Now,
                        Accounttypeid = (int)AccountType.Admin,
                    };

                    _unitOfWork.AspNetUserRepository.Add(aspnetuser);
                    _unitOfWork.Save();

                    Admin admin1 = new()
                    {
                        Aspnetuserid = aspnetuser.Id,
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Email = model.Email,
                        Mobile = phoneNumber,
                        Address1 = model.Address1,
                        Address2 = model.Address2,
                        City = city,
                        Regionid = model.RegionId,
                        Zip = model.Zip,
                        Createdby = aspnetuser.Id,
                        Createddate = DateTime.Now,
                        Roleid = model.RoleId,
                        Altphone = altPhoneNumber,
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
                            _unitOfWork.AdminRegionRepo.Add(adminregion);
                            _unitOfWork.Save();
                        }
                    }


                    TempData["success"] = "Admin Created sucessfully";
                    return RedirectToAction("UserAccess");
                }
                catch (Exception ex)
                {
                    _notyf.Error(ex.Message);
                    model.roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Admin);
                    model.regions = _unitOfWork.RegionRepository.GetAll();
                    return View("Access/CreateAdminAccount", model);
                }

            }

            _notyf.Error("Please ensure all fields are valid.");
            model.roles = _unitOfWork.RoleRepo.Where(role => role.Accounttype == (int)AccountType.Admin);
            model.regions = _unitOfWork.RegionRepository.GetAll();
            return View("Access/CreateAdminAccount", model);
        }

        [RoleAuthorize((int)AllowMenu.AccountAccess)]
        public IActionResult AccountAccess()
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers
                .Where(x => x.Key == "userId")
                .FirstOrDefault().Value);
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            AccountAccessViewModel model = new AccountAccessViewModel();

            IEnumerable<AccountAccessTRow> accessTables = (from r in _unitOfWork.RoleRepo.GetAll()
                                                               //where r.Isdeleted != true
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

        [RoleAuthorize((int)AllowMenu.AccountAccess)]
        public IActionResult EditRole(int roleId)
        {
            Role? role = _unitOfWork.RoleRepo.GetFirstOrDefault(role => role.Roleid == roleId);

            if (role == null)
            {
                _notyf.Error("Role not found");
                return RedirectToAction("AccountAccess");
            }

            IEnumerable<int> list1 = _unitOfWork.RoleMenuRepository.Where(a => a.Roleid == roleId).ToList().Select(x => x.Menuid ?? 0);

            EditAccessViewModel model = new()
            {
                Menus = _unitOfWork.MenuRepository.Where(menu => menu.Accounttype == role.Accounttype),
                selectedMenus = list1,
                RoleId = roleId,
                RoleName = role.Name,
                AccountType = role.Accounttype,
                netRoles = _unitOfWork.AspNetRoleRepository.GetAll(),
            };

            return View("Access/EditAccess", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.AccountAccess)]
        public bool EditAccessSubmit(EditAccessViewModel model)
        {
            try
            {
                int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
                string? adminAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

                if (ModelState.IsValid)
                {
                    ServiceResponse response = _adminService.AdminAccessService.EditAccessSubmit(model);

                    if (response.StatusCode == ResponseCode.Success)
                    {
                        _notyf.Success("Role updated successfully");
                        return true;
                    }

                    _notyf.Error(response.Message);
                    return false;

                }

                _notyf.Error("Fill all details correctly.");
                return false;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        [RoleAuthorize((int)AllowMenu.AccountAccess)]
        public IActionResult DeleteRole(int roleId)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            try
            {

                ServiceResponse response = _adminService.AdminAccessService.DeleteRole(roleId);

                if (response.StatusCode == ResponseCode.Success)
                {
                    TempData["success"] = "Role Deleted Successfully";
                    return RedirectToAction("AccountAccess");
                }

                TempData["error"] = response.Message;
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
            checkboxItems = _unitOfWork.MenuRepository.Where(x => x.Accounttype == type).ToList();

            return Ok(checkboxItems);

        }

        [HttpGet]
        [RoleAuthorize((int)AllowMenu.AccountAccess)]
        public IActionResult CreateAccess(short menuFilter)
        {
            CreateAccessViewModel model = new CreateAccessViewModel();
            model.netRoles = _unitOfWork.AspNetRoleRepository.GetAll();
            return View("Access/CreateAccess", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.AccountAccess)]
        public IActionResult CreateAccess(CreateAccessViewModel model)
        {
            model.netRoles = _unitOfWork.AspNetRoleRepository.GetAll();
            try
            {
                if (ModelState.IsValid)
                {

                    int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
                    string? adminAspId = HttpContext.Request.Headers.Where(x => x.Key == "userAspId").FirstOrDefault().Value;

                    bool isRoleWithSameNameExists = _unitOfWork.RoleRepo.Where(x => x.Name.ToLower().Equals(model.RoleName.ToLower())).Any();

                    if (isRoleWithSameNameExists)
                    {
                        _notyf.Error("Role With same already exists");
                        return View("Access/CreateAccess", model);
                    }
                    Role role = new()
                    {
                        Name = model.RoleName,
                        Accounttype = (short)model.AccountType,
                        Createdby = adminAspId,
                        Createddate = DateTime.Now,
                        Isdeleted = false

                    };

                    _unitOfWork.RoleRepo.Add(role);
                    _unitOfWork.Save();

                    foreach (var menuId in model.selectedMenus)
                    {
                        Rolemenu rolemenu = new Rolemenu();
                        rolemenu.Menuid = menuId;
                        rolemenu.Roleid = role.Roleid;
                        _unitOfWork.RoleMenuRepository.Add(rolemenu);
                    }

                    _unitOfWork.Save();

                    _notyf.Success("Role Created Successfully");
                    return RedirectToAction("AccountAccess");

                }

                _notyf.Error("Please fill neccessary details");
                model.netRoles = _unitOfWork.AspNetRoleRepository.GetAll();
                return View("Access/CreateAccess", model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("CreateAccess");
            }

        }

        //    public IActionResult UserAccesihisPartialTable(int typeId)
        //    {
        //        string? adminName = HttpContext.Request.Headers.Where(a => a.Key == "userName").FirstOrDefault().Value;

        //        List<int> invalidRequestTypes = new List<int>
        //{
        //    (int)RequestStatus.Block,
        //    (int)RequestStatus.Closed,
        //    (int)RequestStatus.Cancelled,
        //    (int)RequestStatus.CancelledByPatient
        //};
        //        IEnumerable<UserAccessTRow> list1 = new List<UserAccessTRow>();
        //        IEnumerable<UserAccessTRow> list22 = new List<UserAccessTRow>();
        //        IEnumerable<UserAccessTRow> list21 = new List<UserAccessTRow>();
        //        IEnumerable<UserAccessTRow> list3 = new List<UserAccessTRow>();

        //        list1 = (from user in _unitOfWork.Aspnetuser.GetAll()
        //                 join s in _unitOfWork.Aspnetroles.GetAll() on user.Roleid equals s.Id into regions
        //                 from phy in regions.DefaultIfEmpty()
        //                 where (user.Roleid == (int)AllowRole.Admin)
        //                 select new UserAccessTRow
        //                 {
        //                     AccountTypeId = (int)user.Roleid,
        //                     AspnetUserId = user.Id,
        //                     AccountType = phy.Name,
        //                     AccountPOC = user.Username.ToString() ?? "",
        //                     Phone = user.Phonenumber ?? "+91 XX XX XX XX XX",
        //                     Status = "Offline",
        //                     OpenRequests = _unitOfWork.Request.GetAll().Where(r => !invalidRequestTypes.Contains(r.Status)).Count(),
        //                 });
        //        DateTime current = DateTime.Now;
        //        var onDutyQuery = from shiftDetail in _unitOfWork.ShiftDetail.GetAll()
        //                          join physician in _unitOfWork.Physician.GetAll() on shiftDetail.Shift.Physicianid equals physician.Physicianid

        //                          where shiftDetail.Shiftdate.Date == current.Date
        //                          && TimeOnly.FromDateTime(current) >= shiftDetail.Starttime
        //                          && TimeOnly.FromDateTime(current) <= shiftDetail.Endtime
        //                          && shiftDetail.Isdeleted != true

        //                          select physician;
        //        list21 = (from user in _unitOfWork.Aspnetuser.GetAll()
        //                  join s in _unitOfWork.Aspnetroles.GetAll() on user.Roleid equals s.Id into regions
        //                  from phy in regions.DefaultIfEmpty()
        //                  join p in onDutyQuery on user.Id equals p.Aspnetuserid

        //                  where user.Roleid == (int)AllowRole.Physician
        //                  select new UserAccessTRow
        //                  {
        //                      AccountTypeId = (int)user.Roleid,
        //                      AspnetUserId = user.Id,
        //                      AccountType = phy.Name,
        //                      AccountPOC = user.Username.ToString() ?? "",
        //                      Phone = user.Phonenumber ?? "+91 XX XX XX XX XX",
        //                      Status = "OnDuty",
        //                      OpenRequests = _unitOfWork.Request.GetAll().Where(r => !invalidRequestTypes.Contains(r.Status) && (r.Physicianid == p.Physicianid)).Count(),
        //                  });


        //        list22 = (from user in _unitOfWork.Aspnetuser.GetAll()
        //                  join s in _unitOfWork.Aspnetroles.GetAll() on user.Roleid equals s.Id into regions
        //                  from phy in regions.DefaultIfEmpty()
        //                  join p in _unitOfWork.Physician.GetAll().Except(onDutyQuery) on user.Id equals p.Aspnetuserid

        //                  where user.Roleid == (int)AllowRole.Physician
        //                  select new UserAccessTRow
        //                  {
        //                      AccountTypeId = (int)user.Roleid,
        //                      AspnetUserId = user.Id,
        //                      AccountType = phy.Name,
        //                      AccountPOC = user.Username.ToString() ?? "",
        //                      Phone = user.Phonenumber ?? "+91 XX XX XX XX XX",
        //                      Status = "OffDuty",
        //                      OpenRequests = _unitOfWork.Request.GetAll().Where(r => !invalidRequestTypes.Contains(r.Status) && (r.Physicianid == p.Physicianid)).Count(),
        //                  });

        //        if (typeId == (int)AllowRole.Admin)
        //        {
        //            list3 = list1;


        //        }
        //        else if (typeId == (int)AllowRole.Physician)
        //        {
        //            list3 = list22.Union(list21);

        //        }
        //        else
        //        {
        //            list3 = list1.Union(list22.Union(list21));
        //        }
        //        UserAccessViewModel model = new()
        //        {
        //            accounttype = typeId,
        //            Accounttypes = _unitOfWork.Aspnetroles.GetAll().ToList(),
        //            username = adminName,
        //            userList = list3,
        //        };
        //        return PartialView("UserAccessPartial", model);
        //    }

        public async Task<IActionResult> UserAccessPartialTable(int pageNo, int accountType)
        {
            int pageNumber = pageNo;
            int pageSize = 5;

            try
            {

                List<int> invalidRequestTypes = new()
                {
                    (int)RequestStatus.Block,
                    (int)RequestStatus.Closed,
                    (int)RequestStatus.Cancelled,
                    (int)RequestStatus.CancelledByPatient,
                };

                IEnumerable<UserAccessTRow> adminList = new List<UserAccessTRow>();
                IEnumerable<UserAccessTRow> phyOnDutyList = new List<UserAccessTRow>();
                IEnumerable<UserAccessTRow> phyOffDutyList = new List<UserAccessTRow>();
                IEnumerable<UserAccessTRow> phyList = new List<UserAccessTRow>();
                IEnumerable<UserAccessTRow> resultList = new List<UserAccessTRow>();


                if (accountType == 0 || accountType == (int)AccountType.Admin)
                {
                    adminList = (from aspUser in _unitOfWork.AspNetUserRepository.GetAll()
                                 where (aspUser.Accounttypeid == (int)AccountType.Admin)
                                 select new UserAccessTRow
                                 {
                                     AccountTypeId = aspUser.Accounttypeid,
                                     AspnetUserId = aspUser.Id,
                                     AccountType = "Admin",
                                     AccountPOC = aspUser.Username.ToString() ?? "",
                                     Phone = aspUser.Phonenumber ?? "+91 XX XX XX XX XX",
                                     Status = "Active",
                                     OpenRequests = _unitOfWork.RequestRepository.GetAll().Where(r => !invalidRequestTypes.Contains(r.Status)).Count(),
                                 }).AsQueryable();
                }

                if (accountType == 0 || accountType == (int)AccountType.Physician)
                {
                    DateTime current = DateTime.Now;
                    var onDutyQuery = from shiftDetail in _unitOfWork.ShiftDetailRepository.GetAll()
                                      join physician in _unitOfWork.PhysicianRepository.GetAll() on shiftDetail.Shift.Physicianid equals physician.Physicianid
                                      where shiftDetail.Shiftdate.Date == current.Date
                                      && TimeOnly.FromDateTime(current) >= shiftDetail.Starttime
                                      && TimeOnly.FromDateTime(current) <= shiftDetail.Endtime
                                      && shiftDetail.Isdeleted != true
                                      select physician;

                    onDutyQuery = onDutyQuery.Distinct();

                    phyOnDutyList = (from user in _unitOfWork.AspNetUserRepository.GetAll()
                                     join p in onDutyQuery on user.Id equals p.Aspnetuserid
                                     where user.Accounttypeid == (int)AccountType.Physician
                                     select new UserAccessTRow
                                     {
                                         AccountTypeId = user.Accounttypeid,
                                         AspnetUserId = user.Id,
                                         AccountType = "Physician",
                                         AccountPOC = user.Username.ToString() ?? "",
                                         Phone = user.Phonenumber ?? "+91 XX XX XX XX XX",
                                         Status = "On Duty",
                                         OpenRequests = _unitOfWork.RequestRepository.GetAll().Where(r => !invalidRequestTypes.Contains(r.Status) && (r.Physicianid == p.Physicianid)).Count(),
                                     });


                    phyOffDutyList = (from user in _unitOfWork.AspNetUserRepository.GetAll()
                                      join p in _unitOfWork.PhysicianRepository.GetAll().Except(onDutyQuery) on user.Id equals p.Aspnetuserid
                                      where user.Accounttypeid == (int)AccountType.Physician
                                      select new UserAccessTRow
                                      {
                                          AccountTypeId = user.Accounttypeid,
                                          AspnetUserId = user.Id,
                                          AccountType = "Physician",
                                          AccountPOC = user.Username.ToString() ?? "",
                                          Phone = user.Phonenumber ?? "+91 XX XX XX XX XX",
                                          Status = "Off Duty",
                                          OpenRequests = _unitOfWork.RequestRepository.GetAll().Where(r => !invalidRequestTypes.Contains(r.Status) && (r.Physicianid == p.Physicianid)).Count(),
                                      });

                    phyList = phyOnDutyList.Union(phyOffDutyList);

                }


                if (accountType == (int)AccountType.Admin)
                {
                    resultList = adminList;

                }
                else if (accountType == (int)AccountType.Physician)
                {
                    resultList = phyList;
                }
                else
                {
                    resultList = adminList.Union(phyList);
                }

                return PartialView("Access/Partial/_UserAccessPartialTable", resultList);

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return PartialView("Access/Partial/_UserAccessPartialTable");
            }
        }

        //public string GetOpenRequests(string aspUserId, int accountTypeId)
        //{
        //    List<int> invalidRequestTypes = new List<int>
        //    {
        //        (int)RequestStatus.Block,
        //        (int)RequestStatus.Closed,
        //        (int)RequestStatus.Cancelled,
        //        (int)RequestStatus.CancelledByPatient
        //    };

        //    switch (accountTypeId)
        //    {
        //        case (int)AccountType.Admin:
        //            Admin? admin = _unitOfWork.AdminRepository.GetFirstOrDefault(admin => admin.Aspnetuserid == aspUserId);
        //            if (admin == null)
        //            {
        //                return "";
        //            }

        //            List<int?> adminRegions = _unitOfWork.AdminRegionRepo.Where(adRe => adRe.Adminid == admin.Adminid).Select(_ => _.Regionid).ToList();
        //            int query = (from req in _unitOfWork.RequestRepository.GetAll()
        //                         join reqCli in _unitOfWork.RequestClientRepository.GetAll() on req.Requestid equals reqCli.Requestid
        //                         where (!invalidRequestTypes.Contains(req.Status))
        //                         && (adminRegions.Contains(reqCli.Regionid))
        //                         select req
        //                         ).Count();

        //            return "";

        //        case (int)AccountType.Physician:

        //            return "";
        //        default:
        //            return "";
        //    }

        //}


        [RoleAuthorize((int)AllowMenu.UserAccess)]
        public IActionResult UserAccess()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            return View("Access/UserAccess", adminName);
        }

        #endregion

        #region Records

        [RoleAuthorize((int)AllowMenu.BlockedHistory)]
        public IActionResult Unblock(int requestId)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(a => a.Key == "userId").FirstOrDefault().Value);
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            try
            {
                ServiceResponse response = _adminService.AdminRecordService.UnBlockRequest(requestId, adminName, adminId);

                if (response.StatusCode == ResponseCode.Success)
                {
                    _notyf.Success(NotificationMessage.REQUEST_UNBLOCKED_SUCCESSFULLY);
                    return RedirectToAction("BlockedHistory");
                }

                _notyf.Error(response.Message);
                return RedirectToAction("BlockedHistory");

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("BlockedHistory");
            }

        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.SMSLogs)]
        public async Task<IActionResult> SMSLogsPartialTable(int pageNo, int roleIdFilter, string receiverName, string mobileNumber, DateTime? createdDate, DateTime? sentDate)
        {

            int pageSize = 10;
            int pageNumber = pageNo;

            LogFilter filter = new LogFilter()
            {
                PageNumber = pageNo,
                PageSize = pageSize,
                RoleId = roleIdFilter,
                ReceiverName = receiverName,
                MobileNumber = mobileNumber,
                CreatedDate = createdDate,
                SentDate = sentDate,
            };

            PagedList<LogTableRow> pagedList = await _adminService.AdminRecordService.GetSMSLogsPaginatedAsync(filter);

            return PartialView("Records/Partial/_SMSLogPartialTable", pagedList);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.EmailLogs)]
        public async Task<IActionResult> EmailLogsPartialTable(int pageNo, int roleIdFilter, string receiverName, string emailAddress, DateTime? createdDate, DateTime? sentDate)
        {
            int pageSize = 10;
            int pageNumber = pageNo;

            LogFilter filter = new LogFilter()
            {
                PageNumber = pageNo,
                PageSize = pageSize,
                RoleId = roleIdFilter,
                ReceiverName = receiverName,
                EmailAddress = emailAddress,
                CreatedDate = createdDate,
                SentDate = sentDate,
            };

            PagedList<LogTableRow> pagedList = await _adminService.AdminRecordService.GetEmailLogsPaginatedAsync(filter);

            return PartialView("Records/Partial/_EmailLogPartialTable", pagedList);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.SearchRecords)]
        public async Task<IActionResult> SearchRecordPartialTable(int pageNo, string patientName, int requestStatus, int requestType, string phoneNumber, DateTime? fromDateOfService, DateTime? toDateOfService, string providerName, string patientEmail)
        {
            SearchRecordFilter searchRecordFilter = new SearchRecordFilter
            {
                PageNumber = pageNo,
                PageSize = 5,
                PatientName = patientName,
                PatientEmail = patientEmail,
                RequestStatus = requestStatus,
                RequestType = requestType,
                PhoneNumber = phoneNumber,
                FromDateOfService = fromDateOfService,
                ToDateOfService = toDateOfService,
                ProviderName = providerName,
            };

            PagedList<SearchRecordTRow> pagedList = await _adminService.AdminRecordService.GetSearchRecordsDataAsync(searchRecordFilter);

            return PartialView("Records/Partial/_SearchRecordPartialTable", pagedList);
        }

        public IActionResult ExportSearchRecordToExcel(string patientName, int requestStatus, int requestType, string phoneNumber, DateTime? fromDateOfService, DateTime? toDateOfService, string providerName, string patientEmail)
        {
            try
            {
                SearchRecordFilter searchRecordFilter = new SearchRecordFilter
                {
                    PatientName = patientName,
                    PatientEmail = patientEmail,
                    RequestStatus = requestStatus,
                    RequestType = requestType,
                    PhoneNumber = phoneNumber,
                    FromDateOfService = fromDateOfService,
                    ToDateOfService = toDateOfService,
                    ProviderName = providerName,
                };

                IEnumerable<SearchRecordTRow> allRequest = _adminService.AdminRecordService.GetSearchRecordsDataUnPaginated(searchRecordFilter);

                if (!allRequest.Any())
                {
                    _notyf.Error("No Request Data For Downloading");
                    return RedirectToAction("SearchRecords");
                }
                DataTable dt = _adminService.AdminRecordService.GetDataTableForSearchRecord(allRequest);

                string fileName = "SearchRecord.xlsx";
                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        return File(stream.ToArray(), "application/vnd.ms-excel", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return RedirectToAction("SearchRecords");
            }
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.SearchRecords)]
        public bool DeleteRequest(int requestId)
        {
            try
            {
                Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(a => a.Requestid == requestId);

                if (req == null)
                {
                    _notyf.Error(NotificationMessage.REQUEST_NOT_FOUND);
                    return false;
                }

                req.Isdeleted = true;
                req.Modifieddate = DateTime.Now;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                _notyf.Success(NotificationMessage.REQUEST_DELETED_SUCCESSFULLY);
                return true;

            }
            catch (Exception ex)
            {
                _notyf.Error(ex.Message);
                return false;
            }
        }

        [RoleAuthorize((int)AllowMenu.SearchRecords)]
        public IActionResult SearchRecords()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            SearchRecordViewModel model = new SearchRecordViewModel()
            {
                UserName = adminName,
                requeststatuses = _unitOfWork.RequestStatusRepository.GetAll(),
                requesttypes = _unitOfWork.RequestTypeRepository.GetAll(),
            };
            return View("Records/SearchRecords", model);
        }


        [RoleAuthorize((int)AllowMenu.EmailLogs)]
        public IActionResult EmailLogs()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            EmailLogsViewModel model = new()
            {
                UserName = adminName,
                roles = _unitOfWork.RoleRepo.GetAll(),
            };
            return View("Records/EmailLogs", model);
        }


        [RoleAuthorize((int)AllowMenu.SMSLogs)]
        public IActionResult SMSLogs()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            SMSLogsViewModel model = new()
            {
                UserName = adminName,
                roles = _unitOfWork.RoleRepo.GetAll(),
            };
            return View("Records/SMSLogs", model);
        }


        [RoleAuthorize((int)AllowMenu.PatientRecords)]
        public IActionResult PatientRecords()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            PatientRecordsViewModel model = new()
            {
                UserName = adminName,
                roles = _unitOfWork.RoleRepo.GetAll(),
            };
            return View("Records/PatientRecords", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.PatientRecords)]
        public async Task<IActionResult> PatientRecordsPartial(int pageNo, string firstName, string lastName, string emailAddress, string phoneNumber)
        {
            int pageSize = 10;

            PatientRecordFilter filter = new PatientRecordFilter()
            {
                PageNumber = pageNo,
                PageSize = pageSize,
                FirstName = firstName,
                LastName = lastName,
                EmailAddress = emailAddress,
                PhoneNumber = phoneNumber,
            };

            PagedList<User> pagedList = await _adminService.AdminRecordService.GetPatientRecordsPaginatedAsync(filter);

            return PartialView("Records/Partial/_PatientRecordPartialTable", pagedList);
        }


        public IActionResult DownloadEncounterPdf(int? requestId)
        {
            try
            {

                EncounterFormViewModel? model = _adminService.AdminProviderService.GetEncounterFormModel(requestId ?? 0, true);

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

        [RoleAuthorize((int)AllowMenu.PatientRecords)]
        public IActionResult Explore(int id)
        {
            IEnumerable<ExploreViewTRow> list = (from r in _unitOfWork.RequestRepository.GetAll()
                                                 join p in _unitOfWork.PhysicianRepository.GetAll() on r.Physicianid equals p.Physicianid
                                                 join file in _unitOfWork.RequestWiseFileRepository.GetAll()
                                         on r.Requestid equals file.Requestid into filesGroup
                                                 where r.Userid == id
                                                 select new ExploreViewTRow
                                                 {
                                                     requestId = r.Requestid,
                                                     patientName = r.Firstname + " " + r.Lastname,
                                                     confirmationNumber = r.Confirmationnumber,
                                                     status = RequestHelper.GetRequestStatusString(r.Status),
                                                     createdAt = r.Createddate,
                                                     concludedDate = DateTime.Now,
                                                     count = filesGroup.Count(),
                                                     finalReport = true,
                                                 });

            PatientRecordsViewModel model = new()
            {
                explores = list,
            };
            return View("Records/PatientExplore", model);
        }


        [RoleAuthorize((int)AllowMenu.BlockedHistory)]
        public IActionResult BlockedHistory()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            return View("Records/BlockedHistory", adminName);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.BlockedHistory)]
        public IActionResult BlockedHistoryPartialTable()
        {
            List<BlockedHistory> list = (from br in _unitOfWork.BlockRequestRepo.GetAll()
                                         where (br.Isactive == true)
                                         join r in _unitOfWork.RequestRepository.GetAll() on br.Requestid equals r.Requestid
                                         join rc in _unitOfWork.RequestClientRepository.GetAll() on r.Requestid equals rc.Requestid
                                         select new BlockedHistory
                                         {
                                             BlockedRequestID = br.Blockrequestid,
                                             RequestId = r.Requestid,
                                             PatientName = rc.Firstname + " " + rc.Lastname,
                                             CreatedDate = br.Createddate,
                                             PhoneNumber = br.Phonenumber,
                                             Email = br.Email,
                                             Notes = "",
                                             IsActive = (bool)br.Isactive
                                         }).ToList();

            return View("Records/Partial/_BlockHistoryPartialTable", list);
        }


        #endregion

        #region Partners

        [RoleAuthorize((int)AllowMenu.Partners)]
        public IActionResult AddBusiness()
        {
            EditBusinessViewModel model = new EditBusinessViewModel()
            {
                regions = _unitOfWork.RegionRepository.GetAll(),
                professions = _unitOfWork.HealthProfessionalTypeRepo.GetAll(),
            };

            return View("Partners/AddBusiness", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.Partners)]
        public bool DeleteBusiness(int vendorId)
        {
            try
            {

                Healthprofessional? vendor = _unitOfWork.HealthProfessionalRepo.GetFirstOrDefault(x => x.Vendorid == vendorId);
                if (vendor == null)
                {
                    _notyf.Error("Cannot Find Vendor. Please try again later.");
                    return false;
                }

                vendor.Isdeleted = true;
                _unitOfWork.HealthProfessionalRepo.Update(vendor);
                _unitOfWork.Save();

                _notyf.Success("Vendor Deleted Successfully");

                return true;
            }
            catch (Exception e)
            {
                _notyf.Error(e.Message);
                return false;
            }

        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.Partners)]
        public IActionResult AddBusiness(EditBusinessViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.CityId)?.Name;
                    string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(x => x.Regionid == model.RegionId)?.Name;
                    Healthprofessional healthprofessional = new()
                    {
                        Vendorname = model.BusinessName,
                        Profession = model.ProfessionId,
                        Faxnumber = model.FaxNumber,
                        Address = model.Street,
                        City = city,
                        State = state,
                        Zip = model.Zip,
                        Regionid = model.RegionId,
                        Createddate = DateTime.Now,
                        Phonenumber = model.PhoneNumber,
                        Businesscontact = model.BusinessContact,
                        Email = model.Email,
                        Isdeleted = false,
                    };

                    _unitOfWork.HealthProfessionalRepo.Add(healthprofessional);
                    _unitOfWork.Save();

                    _notyf.Success("Business Created Successfully");
                    return RedirectToAction("Vendors");
                }

                _notyf.Error("Please ensure all fields are correct.");



            }
            catch (Exception e)
            {
                _notyf.Error(e.Message);
            }

            model.regions = _unitOfWork.RegionRepository.GetAll();
            model.professions = _unitOfWork.HealthProfessionalTypeRepo.GetAll();


            return View("Partners/AddBusiness", model);

        }

        [RoleAuthorize((int)AllowMenu.Partners)]
        public IActionResult EditBusiness(int vendorId)
        {

            Healthprofessional? vendor = _unitOfWork.HealthProfessionalRepo.GetFirstOrDefault(x => x.Vendorid == vendorId);
            int? cityId = vendor?.City == null ? null : _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Name == vendor.City)?.Id;

            EditBusinessViewModel model = new()
            {
                VendorId = vendorId,
                BusinessName = vendor?.Vendorname,
                ProfessionId = vendor?.Profession ?? 0,
                Email = vendor?.Email,
                PhoneNumber = vendor?.Phonenumber,
                FaxNumber = vendor?.Faxnumber,
                BusinessContact = vendor?.Businesscontact,
                Street = vendor?.Address,
                CityId = cityId ?? 0,
                Zip = vendor?.Zip,
                regions = _unitOfWork.RegionRepository.GetAll(),
                professions = _unitOfWork.HealthProfessionalTypeRepo.GetAll(),
                selectedCities = vendor?.Regionid == null ? null : _unitOfWork.CityRepository.Where(city => city.Regionid == vendor.Regionid),
                RegionId = vendor?.Regionid,
            };
            return View("Partners/EditBusiness", model);
        }

        [HttpPost]
        [RoleAuthorize((int)AllowMenu.Partners)]
        public IActionResult EditBusiness(EditBusinessViewModel model)
        {
            try
            {
                Healthprofessional vendor = _unitOfWork.HealthProfessionalRepo.GetFirstOrDefault(x => x.Vendorid == model.VendorId);

                string? city = _unitOfWork.CityRepository.GetFirstOrDefault(city => city.Id == model.CityId)?.Name;
                string? state = _unitOfWork.RegionRepository.GetFirstOrDefault(x => x.Regionid == model.RegionId)?.Name;

                vendor.Vendorname = model.BusinessName;
                vendor.Profession = model.ProfessionId;
                vendor.Email = model.Email;
                vendor.Faxnumber = model.FaxNumber;
                vendor.Phonenumber = model.PhoneNumber;
                vendor.Businesscontact = model.BusinessContact;
                vendor.Address = model.Street;
                vendor.City = city;
                vendor.State = state;
                vendor.Zip = model.Zip;
                vendor.Regionid = model.RegionId;

                _unitOfWork.HealthProfessionalRepo.Update(vendor);
                _unitOfWork.Save();

                _notyf.Success("Vendor Updated Successfully.");
            }
            catch (Exception e)
            {
                _notyf.Error(e.Message);
            }

            return RedirectToAction("Vendors");
        }


        [RoleAuthorize((int)AllowMenu.Partners)]
        public IActionResult Vendors()
        {
            string? adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            VendorViewModel model = new()
            {
                UserName = adminName,
                Healthprofessionaltypes = _unitOfWork.HealthProfessionalTypeRepo.GetAll(),
            };
            return View("Partners/Vendors", model);
        }

        [RoleAuthorize((int)AllowMenu.Partners)]
        public IActionResult BusinessTable(int pageNumber, int pageSize, string vendorName, int professionId)
        {

            IEnumerable<VendorTRow> parseData = (from vendor in _unitOfWork.HealthProfessionalRepo.GetAll()
                                                 join vendorType in _unitOfWork.HealthProfessionalTypeRepo.GetAll() on vendor.Profession equals vendorType.Healthprofessionalid
                                                 where vendor.Isdeleted != true
                                                 && (string.IsNullOrEmpty(vendorName) || vendor.Vendorname.ToLower().Contains(vendorName.ToLower()))
                                                 && (professionId == 0 || vendorType.Healthprofessionalid == professionId)
                                                 select new VendorTRow
                                                 {
                                                     BusinessId = vendor.Vendorid,
                                                     BusinessName = vendor.Vendorname,
                                                     ProfessionId = vendorType.Healthprofessionalid,
                                                     ProfessionName = vendorType.Professionname,
                                                     Email = vendor.Email,
                                                     PhoneNumber = vendor.Phonenumber,
                                                     FaxNumber = vendor.Faxnumber,
                                                     BusinessContact = vendor.Businesscontact
                                                 }).ToList();

            return PartialView("Partners/Partial/_VendorPartialTable", parseData);

        }

        #endregion

        #region HelperFunctions


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

                string? createLink = Url.Action("CreateAccount", "Guest", new { token = createAccToken }, Request.Scheme);
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


        [HttpPost]
        public JsonArray GetBusinessByType(int professionType)
        {
            JsonArray result = new JsonArray();
            IEnumerable<Healthprofessional> businesses = _unitOfWork.HealthProfessionalRepo.Where(prof => prof.Profession == professionType);

            foreach (Healthprofessional business in businesses)
            {
                result.Add(new { businessId = business.Vendorid, businessName = business.Vendorname });
            }

            return result;
        }

        [HttpPost]
        public IEnumerable<Physician> GetPhysicianByPhysicianRegion(int regionId, int? physicianId)
        {
            var result = new JsonArray();

            IEnumerable<int> phyRegions = _unitOfWork.PhysicianRegionRepo.Where(phyReg => phyReg.Regionid == regionId).Select(_ => _.Physicianid);

            List<Physician> physicians = _unitOfWork.PhysicianRepository.Where(phy => phyRegions.Contains(phy.Physicianid)).ToList();

            Physician? removePhy = physicians.SingleOrDefault(p => p.Physicianid == physicianId);
            if (removePhy != null)
            {
                physicians.Remove(removePhy);
            }
            return physicians;
        }

        [HttpPost]
        public JsonArray GetPhysicianByRegion(int regionId, int? physicianId)
        {
            var result = new JsonArray();
            IEnumerable<Physician> physicians;

            if (physicianId != null)
            {
                physicians = _unitOfWork.PhysicianRepository.Where(phy => phy.Regionid == regionId && phy.Physicianid != physicianId);
            }
            else
            {
                physicians = _unitOfWork.PhysicianRepository.Where(phy => phy.Regionid == regionId);
            }


            foreach (Physician physician in physicians)
            {
                result.Add(new { physicianId = physician.Physicianid, physicianName = physician.Firstname + " " + physician.Lastname });
            }

            return result;
        }

        [HttpPost]
        public Healthprofessional? GetBusinessDetailsById(int vendorId)
        {
            if (vendorId <= 0)
            {
                return null;
            }
            Healthprofessional? business = _unitOfWork.HealthProfessionalRepo.GetFirstOrDefault(prof => prof.Vendorid == vendorId);

            return business;
        }

        #endregion

    }
}