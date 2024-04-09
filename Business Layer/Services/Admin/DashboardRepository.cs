﻿using Business_Layer.Services.Admin.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;

namespace Business_Layer.Services.Admin
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;
        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedList<AdminRequest>> GetAdminRequestsAsync(DashboardFilter dashboardParams)
        {
            int pageNumber = dashboardParams.pageNumber;

            if (dashboardParams.pageNumber < 1)
            {
                pageNumber = 1;
            }

            List<short> validRequestTypes = new List<short>();
            switch (dashboardParams.status)
            {
                case (int)DashboardStatus.New:
                    validRequestTypes.Add((short)RequestStatus.Unassigned);
                    break;
                case (int)DashboardStatus.Pending:
                    validRequestTypes.Add((short)RequestStatus.Accepted);
                    break;
                case (int)DashboardStatus.Active:
                    validRequestTypes.Add((short)RequestStatus.MDEnRoute);
                    validRequestTypes.Add((short)RequestStatus.MDOnSite);
                    break;
                case (int)DashboardStatus.Conclude:
                    validRequestTypes.Add((short)RequestStatus.Conclude);
                    break;
                case (int)DashboardStatus.ToClose:
                    validRequestTypes.Add((short)RequestStatus.Cancelled);
                    validRequestTypes.Add((short)RequestStatus.CancelledByPatient);
                    validRequestTypes.Add((short)RequestStatus.Closed);

                    break;
                case (int)DashboardStatus.Unpaid:
                    validRequestTypes.Add((short)RequestStatus.Unpaid);
                    break;
            }

            var query = (from r in _context.Requests
                         where validRequestTypes.Contains(r.Status)
                         && (dashboardParams.RequestTypeFilter == 0 || r.Requesttypeid == dashboardParams.RequestTypeFilter)
                         join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                         where (dashboardParams.RegionFilter == 0 || rc.Regionid == dashboardParams.RegionFilter)
                         && (string.IsNullOrEmpty(dashboardParams.PatientSearchText) || (rc.Firstname + " " + rc.Lastname).ToLower().Contains(dashboardParams.PatientSearchText.ToLower()))
                         join phy in _context.Physicians on r.Physicianid equals phy.Physicianid into phyGroup
                         from phyItem in phyGroup.DefaultIfEmpty()
                         join region in _context.Regions on rc.Regionid equals region.Regionid into regionGroup
                         from regionItem in regionGroup.DefaultIfEmpty()
                         select new AdminRequest
                         {
                             PhysicianId = r.Physicianid,
                             DateOfService = r.Accepteddate,
                             RegionName = regionItem.Name,
                             RequestId = r.Requestid,
                             Email = rc.Email,
                             PatientName = rc.Firstname + " " + rc.Lastname,
                             DateOfBirth = GetPatientDOB(rc),
                             RequestType = r.Requesttypeid,
                             Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                             RequestDate = GetRequestDateWithDiff(r.Createddate),
                             PatientPhone = rc.Phonenumber,
                             PhysicianName = phyItem.Firstname + " " + phyItem.Lastname,
                             Phone = r.Phonenumber,
                             Address = rc.Address,
                             Notes = _context.Requeststatuslogs.Where(log => log.Requestid == r.Requestid).OrderByDescending(_ => _.Createddate).First().Notes,
                         }).AsQueryable();

            return await PagedList<AdminRequest>.CreateAsync(
            query, pageNumber, dashboardParams.pageSize);

        }

        public static string GetRequestDateWithDiff(DateTime requestDate)
        {
            DateTime dateNow = DateTime.Now;
            double difference = dateNow.Subtract(requestDate).TotalMinutes;
            return requestDate.ToString("MMM dd, yyyy") + " ( " + Math.Floor(difference) + " ) mins";
        }


        public List<AdminRequest> GetAllRequestByStatus(int status)
        {

            List<short> validRequestTypes = new List<short>();
            switch (status)
            {
                case (int)DashboardStatus.New:
                    validRequestTypes.Add((short)RequestStatus.Unassigned);
                    break;
                case (int)DashboardStatus.Pending:
                    validRequestTypes.Add((short)RequestStatus.Accepted);
                    break;
                case (int)DashboardStatus.Active:
                    validRequestTypes.Add((short)RequestStatus.MDEnRoute);
                    validRequestTypes.Add((short)RequestStatus.MDOnSite);
                    break;
                case (int)DashboardStatus.Conclude:
                    validRequestTypes.Add((short)RequestStatus.Conclude);
                    break;
                case (int)DashboardStatus.ToClose:
                    validRequestTypes.Add((short)RequestStatus.Cancelled);
                    validRequestTypes.Add((short)RequestStatus.CancelledByPatient);
                    validRequestTypes.Add((short)RequestStatus.Closed);

                    break;
                case (int)DashboardStatus.Unpaid:
                    validRequestTypes.Add((short)RequestStatus.Unpaid);
                    break;
            }

            List<AdminRequest> adminRequests = new List<AdminRequest>();

            adminRequests = (from r in _context.Requests
                             join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                             join p in _context.Physicians on r.Physicianid equals p.Physicianid into subgroup
                             from subitem in subgroup.DefaultIfEmpty()
                             where validRequestTypes.Contains(r.Status)
                             select new AdminRequest
                             {

                                 PhysicianId = r.Physicianid,
                                 DateOfService = r.Accepteddate,
                                 RegionName = rc.Regionid.ToString(),
                                 RequestId = r.Requestid,
                                 Email = rc.Email,
                                 PatientName = rc.Firstname + " " + rc.Lastname,
                                 DateOfBirth = GetPatientDOB(rc),
                                 RequestType = r.Requesttypeid,
                                 Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                 RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                 PatientPhone = rc.Phonenumber,
                                 PhysicianName = subitem.Firstname,
                                 Phone = r.Phonenumber,
                                 Address = rc.Address,
                                 Notes = rc.Notes,
                             }).ToList();

            return adminRequests;
        }


        public List<AdminRequest> GetAdminRequest(int status, int page, DashboardFilter filters)
        {
            int pageNumber = 1;
            if (page > 0)
            {
                pageNumber = page;
            }

            int pageSize = 5;
            List<short> validRequestTypes = new List<short>();
            switch (status)
            {
                case (int)DashboardStatus.New:
                    validRequestTypes.Add((short)RequestStatus.Unassigned);
                    break;
                case (int)DashboardStatus.Pending:
                    validRequestTypes.Add((short)RequestStatus.Accepted);
                    break;
                case (int)DashboardStatus.Active:
                    validRequestTypes.Add((short)RequestStatus.MDEnRoute);
                    validRequestTypes.Add((short)RequestStatus.MDOnSite);
                    break;
                case (int)DashboardStatus.Conclude:
                    validRequestTypes.Add((short)RequestStatus.Conclude);
                    break;
                case (int)DashboardStatus.ToClose:
                    validRequestTypes.Add((short)RequestStatus.Cancelled);
                    validRequestTypes.Add((short)RequestStatus.CancelledByPatient);
                    validRequestTypes.Add((short)RequestStatus.Closed);

                    break;
                case (int)DashboardStatus.Unpaid:
                    validRequestTypes.Add((short)RequestStatus.Unpaid);
                    break;
            }

            List<AdminRequest> adminRequests = new List<AdminRequest>();

            adminRequests = (from r in _context.Requests
                             join rc in _context.Requestclients on r.Requestid equals rc.Requestid
                             join p in _context.Physicians on r.Physicianid equals p.Physicianid into subgroup
                             from subitem in subgroup.DefaultIfEmpty()
                             where validRequestTypes.Contains(r.Status)
                             && (filters.RequestTypeFilter == 0 || r.Requesttypeid == filters.RequestTypeFilter)
                             && (filters.RegionFilter == 0 || rc.Regionid == filters.RegionFilter)
                             && (string.IsNullOrEmpty(filters.PatientSearchText) || (rc.Firstname + " " + rc.Lastname).ToLower().Contains(filters.PatientSearchText.ToLower())
                             )
                             select new AdminRequest
                             {

                                 PhysicianId = r.Physicianid,
                                 DateOfService = r.Accepteddate,
                                 RegionName = rc.Regionid.ToString(),
                                 RequestId = r.Requestid,
                                 Email = rc.Email,
                                 PatientName = rc.Firstname + " " + rc.Lastname,
                                 DateOfBirth = GetPatientDOB(rc),
                                 RequestType = r.Requesttypeid,
                                 Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                 RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                 PatientPhone = rc.Phonenumber,
                                 PhysicianName = subitem.Firstname,
                                 Phone = r.Phonenumber,
                                 Address = rc.Address,
                                 Notes = rc.Notes,
                             }).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return adminRequests;
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

    }
}