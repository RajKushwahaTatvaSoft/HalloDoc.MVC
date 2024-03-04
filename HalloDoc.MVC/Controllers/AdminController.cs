using Business_Layer.Interface;
using Business_Layer.Interface.Admin;
using Business_Layer.Repository.Admin;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Utilities;
using System.IO.Compression;

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

    public class AdminController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IWebHostEnvironment _environment;

        public AdminController(IUnitOfWork unitOfWork, IDashboardRepository dashboard, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _dashboardRepository = dashboard;
            _context = context;
            _environment = environment;
        }

        [HttpPost]
        public ActionResult PartialTable(int status, int page, int typeFilter, string searchFilter)
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
                RegionFilter = 0,
            };

            List<AdminRequest> adminRequests = _dashboardRepository.GetAdminRequest(status, pageNumber, filter);

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.adminRequests = adminRequests;
            model.DashboardStatus = status;
            model.CurrentPage = pageNumber;
            model.filterOptions = filter;

            return PartialView("Partial/PartialTable", model);
        }

        [HttpPost]
        public ActionResult LoadNextPage(int status, int page, int typeFilter, string searchFilter)
        {
            page = page + 1;
            return PartialTable(status, page, typeFilter, searchFilter);
        }

        [HttpPost]
        public ActionResult LoadPreviousPage(int status, int page, int typeFilter, string searchFilter)
        {
            page = page - 1;
            return PartialTable(status, page, typeFilter, searchFilter);
        }


        public IActionResult Dashboard()
        {

            AdminDashboardViewModel model = new AdminDashboardViewModel();
            model.physicians = _context.Physicians;
            model.regions = _context.Regions;
            model.NewReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unassigned);
            model.PendingReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Accepted);
            model.ActiveReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.MDEnRoute) || (r.Status == (short)RequestStatus.MDOnSite));
            model.ConcludeReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Conclude);
            model.ToCloseReqCount = _unitOfWork.RequestRepository.Count(r => (r.Status == (short)RequestStatus.Cancelled) || (r.Status == (short)RequestStatus.CancelledByPatient) || (r.Status == (short)RequestStatus.Closed));
            model.UnpaidReqCount = _unitOfWork.RequestRepository.Count(r => r.Status == (short)RequestStatus.Unpaid);
            model.regions = _context.Regions;
            model.physicians = _context.Physicians;
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


        [HttpPost]
        public bool CancelCaseModal(int reason, string notes, int requestid)
        {
            int adminId = 1;
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
                    Physicianid = req.Physicianid,
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

            return false;

        }

        [HttpPost]
        public bool AssignCaseModal(string notes, int requestid, int physicianid)
        {
            int adminId = 1;
            if(requestid == null || requestid <= 0 || physicianid == null ||  physicianid <= 0)
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
                    Physicianid = req.Physicianid,
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

            return false;

        }

        [HttpPost]
        public bool BlockCaseModal(string reason, int requestid)
        {
            int adminId = 1;
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
                    Requestid = reqCli.Requestid.ToString(),
                    Createddate = DateTime.Now,
                    Isactive=true,
                };

                _context.Blockrequests.Add(blockrequest);
                _context.SaveChanges();

                TempData["success"] = "Request Successfully Blocked";
                return true;
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error Occured while blocking request.";
                return false;
            }
        }

        public IActionResult ViewCase(int Requestid)
        {
            if (Requestid == null)
            {
                return View("Error");
            }

            Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req=> req.Requestid == Requestid);
            Requestclient client = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqCli => reqCli.Requestid == Requestid);

            ViewCaseViewModel model = new();

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
            model.regions = _context.Regions;
            model.physicians = _context.Physicians;
            model.casetags = _unitOfWork.CaseTagRepository.GetAll();

            return View("Action/ViewCase", model);
        }

        public int GetDashboardStatus(int requestStatus)
        {
            if(requestStatus == (int) RequestStatus.Unassigned)
            {
                return  (int)DashboardStatus.New;
            }
            else if (requestStatus == (int)RequestStatus.Accepted)
            {
                return (int)DashboardStatus.Pending;
            }
            else if (requestStatus == (int)RequestStatus.MDEnRoute || requestStatus == (int)RequestStatus.MDOnSite )
            {
                return (int)DashboardStatus.Active;
            }
            else if (requestStatus == (int)RequestStatus.Conclude)
            {
                return (int)DashboardStatus.Conclude;
            }
            else if (requestStatus == (int)RequestStatus.Cancelled  || requestStatus == (int)RequestStatus.Closed || requestStatus == (int)RequestStatus.CancelledByPatient)
            {
                return (int)DashboardStatus.ToClose;
            }
            else if (requestStatus == (int)RequestStatus.Unpaid)
            {
                return (int)DashboardStatus.Unpaid;
            }

            return -1;
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
            IEnumerable<Requeststatuslog> logs = _unitOfWork.RequestStatusLogRepository.Where(log => log.Requestid == Requestid);
            Requestnote notes = _context.Requestnotes.FirstOrDefault(notes => notes.Requestid == Requestid);

            ViewNotesViewModel model = new ViewNotesViewModel();

            if(notes != null)
            {
                model.AdminNotes = notes.Adminnotes;
                model.PhysicianNotes = notes.Physiciannotes;
            }

            return View("Action/ViewNotes",model);
        }

        [HttpPost]
        public IActionResult ViewNotes(ViewNotesViewModel vnvm)
        {

            int adminId = 1;
            string adminAspId = "061d38d4-2b2f-48f6-ad21-5a80db6c4e69";
            Requestnote oldnote = _context.Requestnotes.FirstOrDefault( rn => rn.Requestid == vnvm.RequestId);

            if(oldnote != null)
            {
                oldnote.Adminnotes = vnvm.AdminNotes;
                oldnote.Modifieddate = DateTime.Now;
                oldnote.Modifiedby = adminAspId;

                _context.Requestnotes.Update(oldnote);
                _context.SaveChanges();

            }
            else
            {
                Requestnote curReqNote= new Requestnote()
                {
                    Requestid = vnvm.RequestId,
                    Adminnotes = vnvm.AdminNotes,
                    Createdby = adminAspId,
                    Createddate = DateTime.Now,
                };

                _context.Requestnotes.Add(curReqNote);
                _context.SaveChanges();
            }

            return ViewNotes(vnvm.RequestId);
        }

        public IActionResult ViewUploads(int Requestid)
        {
            Request req = _unitOfWork.RequestRepository.GetFirstOrDefault(req => req.Requestid == Requestid) ;
            if(req == null)
            {
                return View("Error");
            }

            Requestclient reqCli = _unitOfWork.RequestClientRepository.GetFirstOrDefault(reqcli => reqcli.Requestid == req.Requestid);
            if(reqCli == null)
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
            };

            return View("Action/ViewUploads",model);
        }

        [HttpPost]
        public IActionResult ViewUploads(ViewUploadsViewModel uploadsVM)
        {

            return View("Action/ViewUploads");
        }

    }
}
