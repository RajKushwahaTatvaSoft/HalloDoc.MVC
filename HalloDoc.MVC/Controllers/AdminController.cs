using Business_Layer.Interface;
using Business_Layer.Interface.AdminInterface;
using Data_Layer.CustomModels;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Data_Layer.ViewModels.Admin;
using HalloDoc.MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using static NuGet.Packaging.PackagingConstants;

namespace HalloDoc.MVC.Controllers
{
    public enum TypeFilter
    {
        All = 0,
        Patient = 1,
        FamilyFriend = 2,
        Concierge = 3,
        Business = 4,
        VIP = 5,
    }

    [CustomAuthorize((int)AllowRole.Admin)]
    public class AdminController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public AdminController(IUnitOfWork unitOfWork, IDashboardRepository dashboard, IWebHostEnvironment environment, IConfiguration config, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _dashboardRepository = dashboard;
            _environment = environment;
            _config = config;
            _context = context;
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


        [HttpPost]
        public ActionResult PartialTable(int status, int page, int typeFilter, string searchFilter, int regionFilter)
        {

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
            };

            List<AdminRequest> adminRequests = _dashboardRepository.GetAdminRequest(status, pageNumber, filter);

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.adminRequests = adminRequests;
            model.DashboardStatus = status;
            model.CurrentPage = pageNumber;
            model.filterOptions = filter;

            return PartialView("Partial/PartialTable", model);
        }

        public IActionResult Dashboard()
        {
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

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
            return View("Dashboard/Dashboard", model);

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

        public static string GetRequestType(Request request)
        {
            switch (request.Requesttypeid)
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

        public IActionResult Orders(int requestId)
        {

            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;

            SendOrderViewModel model = new SendOrderViewModel();
            model.professionalTypeList = _unitOfWork.HealthProfessionalTypeRepo.GetAll();
            model.RequestId = requestId;
            model.UserName = adminName;

            return View("Action/Orders", model);
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


        public IActionResult NewRequestStatusView()
        {
            return View("StatusPartial/NewRequestStatusView");
        }
        public IActionResult Profile()
        {
            return View("Dashboard/Profile");
        }
        public IActionResult Providers()
        {
            return View("Dashboard/Providers");
        }
        public IActionResult Partners()
        {
            return View("Dashboard/Partners");
        }

        public IActionResult ProviderLocation()
        {
            return View("Dashboard/ProviderLocation");
        }

        public IActionResult Records()
        {
            return View("Dashboard/Records");
        }

        public IActionResult Access()
        {
            return View("Dashboard/Access");
        }

        public static string GenerateSHA256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var hashEngine = SHA256.Create())
            {
                var hashedBytes = hashEngine.ComputeHash(bytes, 0, bytes.Length);
                var sb = new StringBuilder();
                foreach (var b in hashedBytes)
                {
                    var hex = b.ToString("x2");
                    sb.Append(hex);
                }
                return sb.ToString();
            }
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


        [HttpPost]
        public bool CancelCaseModal(int reason, string notes, int requestid)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            try
            {
                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);
                req.Status = (short)RequestStatus.Cancelled;
                req.Modifieddate = DateTime.Now;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestid,
                    Status = (short)RequestStatus.Cancelled,
                    Adminid = adminId,
                    Notes = notes,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Cancelled";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while cancelling request.";
                return false;
            }

        }

        [HttpPost]
        public bool AssignCaseModal(string notes, int requestid, int physicianid)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);

            if (requestid == null || requestid <= 0 || physicianid == null || physicianid <= 0)
            {
                TempData["error"] = "Error occured while assigning request.";
                return false;
            }

            try
            {
                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);
                req.Status = (short)RequestStatus.Accepted;
                req.Modifieddate = DateTime.Now;
                req.Physicianid = physicianid;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestid,
                    Status = (short)RequestStatus.Accepted,
                    Adminid = adminId,
                    Notes = notes,
                    Transtophysicianid = req.Physicianid,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();


                TempData["success"] = "Request Successfully Assigned.";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while assigning request.";
                return false;
            }


        }

        [HttpPost]
        public bool TransferCaseModal(string notes, int requestid, int physicianid)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            if (requestid == null || requestid <= 0 || physicianid == null || physicianid <= 0)
            {
                TempData["error"] = "Error occured while assigning request.";
                return false;
            }
            try
            {

                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);
                req.Status = (short)RequestStatus.Accepted;
                req.Modifieddate = DateTime.Now;
                req.Physicianid = physicianid;

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestid,
                    Status = (short)RequestStatus.Accepted,
                    Adminid = adminId,
                    Notes = notes,
                    Transtophysicianid = physicianid,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();


                TempData["success"] = "Request Successfully Assigned.";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while assigning request.";
                return false;
            }

        }

        [HttpPost]
        public bool BlockCaseModal(string reason, int requestid)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            try
            {
                Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);
                req.Status = (short)RequestStatus.Block;
                req.Modifieddate = DateTime.Now;

                _unitOfWork.RequestRepository.Update(req);
                _unitOfWork.Save();

                Requeststatuslog reqStatusLog = new Requeststatuslog()
                {
                    Requestid = requestid,
                    Status = (short)RequestStatus.Block,
                    Adminid = adminId,
                    Notes = reason,
                    Physicianid = req.Physicianid,
                    Createddate = DateTime.Now,
                };

                _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                _unitOfWork.Save();

                Requestclient reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == requestid);

                Blockrequest blockrequest = new Blockrequest()
                {
                    Phonenumber = reqCli.Phonenumber,
                    Email = reqCli.Email,
                    Reason = reason,
                    Requestid = requestid.ToString(),
                    Createddate = DateTime.Now,
                    Isactive = true,
                };

                _unitOfWork.BlockRequestRepo.Add(blockrequest);
                _unitOfWork.Save();

                TempData["success"] = "Request Successfully Blocked";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while blocking request.";
                return false;
            }
        }

        [HttpPost]
        public bool ClearCaseModal(int requestid)
        {
            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            if (adminId != null)
            {
                try
                {
                    Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == requestid);

                    req.Status = (short)RequestStatus.Clear;
                    req.Modifieddate = DateTime.Now;

                    Requeststatuslog reqStatusLog = new Requeststatuslog()
                    {
                        Requestid = requestid,
                        Status = (short)RequestStatus.Clear,
                        Adminid = adminId,
                        Notes = "Admin cleared this request",
                        Createddate = DateTime.Now,
                    };

                    _unitOfWork.RequestRepository.Update(req);
                    _unitOfWork.Save();

                    _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);
                    _unitOfWork.Save();

                    TempData["success"] = "Request Successfully transferred";
                    return true;
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Error Occured while transferring request.";
                    return false;
                }
            }
            else
            {
                TempData["error"] = "Admin Not Found";
                return false;
            }
        }

        public bool SendAgreementMail(string phoneNumber, string email, int requestid)
        {
            try
            {
                string encryptedId = EncryptionService.Encrypt(requestid.ToString());
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

                mailMessage.To.Add(email);

                client.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

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

            return View("Action/ViewCase", model);
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

        public IActionResult ViewNotes(int Requestid)
        {

            int adminId = Convert.ToInt32(HttpContext.Request.Headers.Where(x => x.Key == "userId").FirstOrDefault().Value);
            string adminName = HttpContext.Request.Headers.Where(x => x.Key == "userName").FirstOrDefault().Value;


            IEnumerable<TransferNotesLog> logs = (from rsl in _context.Requeststatuslogs
                                                  join a in _context.Admins on rsl.Adminid equals a.Adminid into groupedAdmin
                                                  from subAdmin in groupedAdmin.DefaultIfEmpty()
                                                  select new TransferNotesLog
                                                  {
                                                      Status = rsl.Status,
                                                      PhysicianId = rsl.Physicianid,
                                                      PhysicianName = "",
                                                      AdminId = rsl.Adminid,
                                                      AdminName = subAdmin.Firstname + " " + subAdmin.Lastname,
                                                      Notes = rsl.Notes,
                                                      CreatedDate = rsl.Createddate
                                                  });

            Requestnote notes = _unitOfWork.RequestNoteRepository.GetFirstOrDefault(notes => notes.Requestid == Requestid);

            ViewNotesViewModel model = new ViewNotesViewModel();

            model.UserName = adminName;



            if (notes != null)
            {
                model.AdminNotes = notes.Adminnotes;
                model.PhysicianNotes = notes.Physiciannotes;
            }

            return View("Action/ViewNotes", model);
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

                _unitOfWork.RequestNoteRepository.Update(oldnote);
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

            return View("Action/ViewUploads", model);
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

        public IActionResult EncounterForm()
        {
            return View("Action/EncounterForm");
        }
    }
}
