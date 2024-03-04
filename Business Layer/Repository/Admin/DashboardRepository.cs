using Business_Layer.Interface.Admin;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels.Admin;

namespace Business_Layer.Repository.Admin
{
    public enum RequestStatus
    {
        Unassigned = 1,
        Accepted = 2,
        Cancelled = 3,
        MDEnRoute = 4,
        MDOnSite = 5,
        Conclude = 6,
        CancelledByPatient = 7,
        Closed = 8,
        Unpaid = 9,
        Clear = 10,
    }

    public enum DashboardStatus
    {
        New = 1,
        Pending = 2,
        Active = 3,
        Conclude = 4,
        ToClose = 5,
        Unpaid = 6,
    }

    public enum RequestType
    {
        Business = 1,
        Patient = 2,
        Family = 3,
        Concierge = 4
    }

    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;
        public DashboardRepository(ApplicationDbContext context)
        {
            _context = context;
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
                             where (validRequestTypes.Contains(r.Status))
                             && (filters.RequestTypeFilter == 0 ? true : r.Requesttypeid == filters.RequestTypeFilter)
                             && ((filters.PatientSearchText == null || filters.PatientSearchText == "") ?
                             true : (rc.Firstname.Contains(filters.PatientSearchText) || rc.Lastname.Contains(filters.PatientSearchText))
                             )
                             select new AdminRequest
                             {
                                 RequestId = r.Requestid,
                                 Email = rc.Email,
                                 PatientName = rc.Firstname + " " + rc.Lastname,
                                 DateOfBirth = GetPatientDOB(rc),
                                 RequestType = r.Requesttypeid,
                                 Requestor = GetRequestType(r) + " " + r.Firstname + " " + r.Lastname,
                                 RequestDate = r.Createddate.ToString("MMM dd, yyyy"),
                                 PatientPhone = rc.Phonenumber,
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
