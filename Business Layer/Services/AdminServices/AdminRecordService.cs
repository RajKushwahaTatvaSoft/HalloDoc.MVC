using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels.TableRow.Admin;
using Data_Layer.CustomModels;
using Data_Layer.CustomModels.Filter;
using System.Data;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;
using Org.BouncyCastle.Utilities;

namespace Business_Layer.Services.AdminServices
{
    public class AdminRecordService : IAdminRecordService
    {
        private readonly IUnitOfWork _unitOfWork;
        public AdminRecordService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<SearchRecordTRow> GetSearchRecordsDataUnPaginated(SearchRecordFilter searchRecordFilter)
        {
            IQueryable<SearchRecordTRow> query = (from r in _unitOfWork.RequestRepository.GetAll()
                                                  join rc in _unitOfWork.RequestClientRepository.GetAll() on r.Requestid equals rc.Requestid
                                                  join rnote in _unitOfWork.RequestNoteRepository.GetAll() on r.Requestid equals rnote.Requestid into noteGroup
                                                  from noteItem in noteGroup.DefaultIfEmpty()
                                                  join rs in _unitOfWork.RequestStatusRepository.GetAll() on r.Status equals rs.Statusid
                                                  join phy in _unitOfWork.PhysicianRepository.GetAll() on r.Physicianid equals phy.Physicianid into phyGroup
                                                  from phyItem in phyGroup.DefaultIfEmpty()
                                                  where (string.IsNullOrEmpty(searchRecordFilter.PatientName) || (rc.Firstname + " " + rc.Lastname).ToLower().Contains(searchRecordFilter.PatientName.ToLower()))
                                                  && (searchRecordFilter.RequestStatus == 0 || r.Status == searchRecordFilter.RequestStatus)
                                                  && (searchRecordFilter.RequestType == 0 || r.Requesttypeid == searchRecordFilter.RequestType)
                                                  && (string.IsNullOrEmpty(searchRecordFilter.PhoneNumber) || rc.Phonenumber.ToLower().Contains(searchRecordFilter.PhoneNumber.ToLower()))
                                                  && (string.IsNullOrEmpty(searchRecordFilter.ProviderName) || (phyItem.Firstname + " " + phyItem.Lastname).ToLower().Contains(searchRecordFilter.ProviderName.ToLower()))
                                                  && (string.IsNullOrEmpty(searchRecordFilter.PatientEmail) || rc.Email.ToLower().Contains(searchRecordFilter.PatientEmail.ToLower()))
                                                  && ((searchRecordFilter.FromDateOfService == null) || r.Accepteddate >= searchRecordFilter.FromDateOfService.Value.Date)
                                                  && ((searchRecordFilter.ToDateOfService == null) || r.Accepteddate <= searchRecordFilter.ToDateOfService.Value.Date)
                                                  select new SearchRecordTRow
                                                  {
                                                      RequestId = r.Requestid,
                                                      PatientName = rc.Firstname + " " + rc.Lastname,
                                                      Requestor = RequestHelper.GetRequestType(r.Requesttypeid),
                                                      DateOfService = r.Accepteddate,
                                                      CloseCaseDate = _unitOfWork.RequestStatusLogRepository.GetAll().AsEnumerable().FirstOrDefault(rstaus => rstaus.Requestid == r.Requestid && rstaus.Status == (int)RequestStatus.Closed).Createddate,
                                                      Email = rc.Email ?? "",
                                                      PhoneNumber = rc.Phonenumber ?? "",
                                                      Address = rc.Address ?? "",
                                                      Zip = rc.Zipcode ?? "",
                                                      RequestStatus = rs.Statusname,
                                                      Physician = phyItem.Firstname + " " + phyItem.Lastname,
                                                      PhysicianNote = noteItem.Physiciannotes,
                                                      AdminNote = noteItem.Adminnotes,
                                                      CancelledByPhysicianNote = "",
                                                      PatientNote = "",
                                                  });

            return query;
        }

        public async Task<PagedList<SearchRecordTRow>> GetSearchRecordsDataAsync(SearchRecordFilter searchRecordFilter)
        {
            int pageSize = searchRecordFilter.PageSize < 1 ? 1 : searchRecordFilter.PageSize;
            int pageNumber = searchRecordFilter.PageNumber < 1 ? 1 : searchRecordFilter.PageNumber;

            IQueryable<SearchRecordTRow> query = (from r in _unitOfWork.RequestRepository.GetAll()
                                                  join rc in _unitOfWork.RequestClientRepository.GetAll() on r.Requestid equals rc.Requestid
                                                  join rnote in _unitOfWork.RequestNoteRepository.GetAll() on r.Requestid equals rnote.Requestid into noteGroup
                                                  from noteItem in noteGroup.DefaultIfEmpty()
                                                  join rs in _unitOfWork.RequestStatusRepository.GetAll() on r.Status equals rs.Statusid
                                                  join phy in _unitOfWork.PhysicianRepository.GetAll() on r.Physicianid equals phy.Physicianid into phyGroup
                                                  from phyItem in phyGroup.DefaultIfEmpty()
                                                  where (string.IsNullOrEmpty(searchRecordFilter.PatientName) || (rc.Firstname + " " + rc.Lastname).ToLower().Contains(searchRecordFilter.PatientName.ToLower()))
                                                  && (searchRecordFilter.RequestStatus == 0 || r.Status == searchRecordFilter.RequestStatus)
                                                  && (searchRecordFilter.RequestType == 0 || r.Requesttypeid == searchRecordFilter.RequestType)
                                                  && (string.IsNullOrEmpty(searchRecordFilter.PhoneNumber) || rc.Phonenumber.ToLower().Contains(searchRecordFilter.PhoneNumber.ToLower()))
                                                  && (string.IsNullOrEmpty(searchRecordFilter.ProviderName) || (phyItem.Firstname + " " + phyItem.Lastname).ToLower().Contains(searchRecordFilter.ProviderName.ToLower()))
                                                  && (string.IsNullOrEmpty(searchRecordFilter.PatientEmail) || rc.Email.ToLower().Contains(searchRecordFilter.PatientEmail.ToLower()))
                                                  && ((searchRecordFilter.FromDateOfService == null) || r.Accepteddate >= searchRecordFilter.FromDateOfService.Value.Date)
                                                  && ((searchRecordFilter.ToDateOfService == null) || r.Accepteddate <= searchRecordFilter.ToDateOfService.Value.Date)
                                                  select new SearchRecordTRow
                                                  {
                                                      RequestId = r.Requestid,
                                                      PatientName = rc.Firstname + " " + rc.Lastname,
                                                      Requestor = RequestHelper.GetRequestType(r.Requesttypeid),
                                                      DateOfService = r.Accepteddate,
                                                      CloseCaseDate = _unitOfWork.RequestStatusLogRepository.GetAll().AsEnumerable().FirstOrDefault(rstaus => rstaus.Requestid == r.Requestid && rstaus.Status == (int)RequestStatus.Closed).Createddate,
                                                      Email = rc.Email ?? "",
                                                      PhoneNumber = rc.Phonenumber ?? "",
                                                      Address = rc.Address ?? "",
                                                      Zip = rc.Zipcode ?? "",
                                                      RequestStatus = rs.Statusname,
                                                      Physician = phyItem.Firstname + " " + phyItem.Lastname,
                                                      PhysicianNote = noteItem.Physiciannotes,
                                                      AdminNote = noteItem.Adminnotes,
                                                      CancelledByPhysicianNote = "",
                                                      PatientNote = "",
                                                  }).AsQueryable();

            PagedList<SearchRecordTRow> pagedList = await PagedList<SearchRecordTRow>.CreateAsync(
            query, pageNumber, pageSize);

            return pagedList;
        }

        public DataTable GetDataTableForSearchRecord(IEnumerable<SearchRecordTRow> requestList)
        {

            DataTable dt = new DataTable();
            dt.TableName = "Search Record Data";

            dt.Columns.Add("Patient Name", typeof(string));
            dt.Columns.Add("Requestor", typeof(string));
            dt.Columns.Add("Date Of Service", typeof(string));
            dt.Columns.Add("Close Case Date", typeof(string));
            dt.Columns.Add("Email", typeof(string));
            dt.Columns.Add("Phone Number", typeof(string));
            dt.Columns.Add("Address", typeof(string));
            dt.Columns.Add("Zip", typeof(string));
            dt.Columns.Add("Request Status", typeof(string));
            dt.Columns.Add("Physician", typeof(string));
            dt.Columns.Add("Physician Note", typeof(string));
            dt.Columns.Add("Cancelled By Provider Note", typeof(string));
            dt.Columns.Add("Admin Note", typeof(string));
            dt.Columns.Add("Patient Note", typeof(string));

            foreach (SearchRecordTRow searchRecord in requestList)
            {
                dt.Rows.Add(
                    searchRecord.PatientName,
                    searchRecord.Requestor,
                    searchRecord.DateOfService,
                    searchRecord.CloseCaseDate,
                    searchRecord.Email,
                    searchRecord.PhoneNumber,
                    searchRecord.Address,
                    searchRecord.Zip,
                    searchRecord.RequestStatus,
                    searchRecord.Physician,
                    searchRecord.PhysicianNote,
                    searchRecord.CancelledByPhysicianNote,
                    searchRecord.AdminNote,
                    searchRecord.PatientNote
                    );
            }

            dt.AcceptChanges();

            return dt;
        }


        public async Task<PagedList<LogTableRow>> GetSMSLogsPaginatedAsync(LogFilter filter)
        {

            IQueryable<LogTableRow> query = (from log in _unitOfWork.SMSLogRepository.GetAll()
                                             where (filter.RoleId == 0 || log.Roleid == filter.RoleId)
                                             && (string.IsNullOrEmpty(filter.MobileNumber) || log.Mobilenumber == filter.MobileNumber)
                                             && (filter.CreatedDate == null || log.Createdate.Date == filter.CreatedDate.Value.Date)
                                             && (filter.SentDate == null || log.Sentdate == null ? true : log.Sentdate.Value.Date == filter.SentDate.Value.Date)
                                             select new LogTableRow
                                             {
                                                 RecipientName = log.Recipientname,
                                                 MobileNumber = log.Mobilenumber,
                                                 Action = log.Action.ToString(),
                                                 RoleName = log.Roleid.ToString(),
                                                 CreatedDate = log.Createdate,
                                                 SentDate = log.Sentdate,
                                                 SentTries = log.Senttries,
                                                 IsSent = log.Issmssent ?? false,
                                                 ConfirmationNumber = log.Confirmationnumber,
                                             }).AsQueryable();

            PagedList<LogTableRow> pagedList = await PagedList<LogTableRow>.CreateAsync(
            query, filter.PageNumber, filter.PageSize);

            return pagedList;

        }
        public async Task<PagedList<LogTableRow>> GetEmailLogsPaginatedAsync(LogFilter filter)
        {

            IQueryable<LogTableRow> query = (from log in _unitOfWork.EmailLogRepository.GetAll()
                                             where (filter.RoleId == 0 || log.Roleid == filter.RoleId)
                                                          && (string.IsNullOrEmpty(filter.EmailAddress) || log.Emailid == filter.EmailAddress)
                                                          && (filter.CreatedDate == null || log.Createdate.Date == filter.CreatedDate.Value.Date)
                                                          && (filter.SentDate == null || log.Sentdate == null ? true : log.Sentdate.Value.Date == filter.SentDate.Value.Date)
                                             select new LogTableRow
                                             {
                                                 RecipientName = log.Recipientname,
                                                 Email = log.Emailid,
                                                 Action = log.Subjectname,
                                                 RoleName = log.Roleid.ToString(),
                                                 CreatedDate = log.Createdate,
                                                 SentDate = log.Sentdate,
                                                 SentTries = log.Senttries ?? 1,
                                                 IsSent = log.Isemailsent ?? false,
                                                 ConfirmationNumber = log.Confirmationnumber,
                                             }).AsQueryable();

            PagedList<LogTableRow> pagedList = await PagedList<LogTableRow>.CreateAsync(
            query, filter.PageNumber, filter.PageSize);

            return pagedList;
        }
        public async Task<PagedList<User>> GetPatientRecordsPaginatedAsync(PatientRecordFilter filter)
        {

            IQueryable<User> logs = _unitOfWork.UserRepository.Where(user =>
            (string.IsNullOrEmpty(filter.FirstName) || user.Firstname.ToLower().Contains(filter.FirstName.ToLower()))
            && (string.IsNullOrEmpty(filter.LastName) || string.IsNullOrEmpty(user.Lastname) || user.Lastname.ToLower().Contains(filter.LastName.ToLower()))
            && (string.IsNullOrEmpty(filter.EmailAddress) || user.Email.ToLower().Contains(filter.EmailAddress.ToLower()))
            && (string.IsNullOrEmpty(filter.PhoneNumber) || string.IsNullOrEmpty(user.Mobile) || user.Mobile.ToLower().Contains(filter.PhoneNumber))).AsQueryable();

            PagedList<User> pagedList = await PagedList<User>.CreateAsync(
            logs, filter.PageNumber, filter.PageSize);

            return pagedList;
        }

        public async Task<PagedList<BlockedHistory>> GetBlockedHistoryRecordsPaginatedAsync(int pageNumber, int pageSize)
        {

            IQueryable<BlockedHistory> list = (from br in _unitOfWork.BlockRequestRepo.GetAll()
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
                                                    Notes = br.Reason,
                                                    IsActive = br.Isactive ?? false,
                                                });

            PagedList<BlockedHistory> pagedList = await PagedList<BlockedHistory>.CreateAsync(
            list, pageNumber, pageSize);

            return pagedList;

        }


        public ServiceResponse UnBlockRequest(int requestId, string adminName, int adminId)
        {

            DateTime currentTime = DateTime.Now;
            Blockrequest? blockRequest = _unitOfWork.BlockRequestRepo.GetFirstOrDefault(b => b.Requestid == requestId);
            Request? request = _unitOfWork.RequestRepository.GetFirstOrDefault(b => b.Requestid == requestId);

            if (blockRequest == null || request == null)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = NotificationMessage.REQUEST_NOT_FOUND,
                };
            }

            if (blockRequest.Isactive == false && request.Status == (int)RequestStatus.Unassigned)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = NotificationMessage.REQUEST_ALREADY_BLOCKED,
                };
            }

            blockRequest.Isactive = false;
            blockRequest.Modifieddate = currentTime;

            request.Status = (short)RequestStatus.Unassigned;
            request.Modifieddate = currentTime;

            _unitOfWork.RequestRepository.Update(request);
            _unitOfWork.BlockRequestRepo.Update(blockRequest);

            string logNotes = adminName + " unblocked this request on " + currentTime.ToString("MM/dd/yyyy") + " at " + currentTime.ToString("HH:mm:ss");

            Requeststatuslog reqStatusLog = new Requeststatuslog()
            {
                Requestid = requestId,
                Status = (short)RequestStatus.Unassigned,
                Adminid = adminId,
                Notes = logNotes,
                Createddate = currentTime,
            };
            _unitOfWork.RequestStatusLogRepository.Add(reqStatusLog);

            _unitOfWork.Save();

            return new ServiceResponse
            {
                StatusCode = ResponseCode.Success,
            };
        }

        public ServiceResponse DeleteRequest(int requestId)
        {

            Request? req = _unitOfWork.RequestRepository.GetFirstOrDefault(a => a.Requestid == requestId);

            if (req == null)
            {
                return new ServiceResponse
                {
                    StatusCode = ResponseCode.Error,
                    Message = NotificationMessage.REQUEST_NOT_FOUND,
                };
            }

            req.Isdeleted = true;
            req.Modifieddate = DateTime.Now;

            _unitOfWork.RequestRepository.Update(req);
            _unitOfWork.Save();

            return new ServiceResponse { StatusCode = ResponseCode.Success, };
        }

    }
}
