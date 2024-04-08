using Business_Layer.Services.Patient.Interface;
using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;

namespace Business_Layer.Services.Patient
{
    public class PatientDashboardRepository : IPatientDashboardRepository
    {
        private readonly ApplicationDbContext _context;
        public PatientDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public User GetUserWithID(int userid)
        {
            User user = _context.Users.FirstOrDefault(u => u.Userid == userid);
            return user;
        }

        public async Task<PagedList<PatientDashboardRequest>> GetPatientRequestsAsync(int userId, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            var query = (from r in _context.Requests
                         where r.Createduserid == userId
                         select new PatientDashboardRequest
                         {
                             RequestId = r.Requestid,
                             RequestStatus = RequestHelper.GetRequestStatusString(r.Status),
                             CreatedDate = r.Createddate,
                             FileCount = _context.Requestwisefiles.Count(file => file.Requestid == r.Requestid),
                         }).AsQueryable();

            return await PagedList<PatientDashboardRequest>.CreateAsync(
            query, pageNumber, pageSize);

        }


        public PatientDashboardViewModel FetchDashboardDetails(int userId)
        {
            User user = GetUserWithID(userId);

            PatientDashboardViewModel dashboardVM = new PatientDashboardViewModel();
            dashboardVM.UserId = user.Userid;
            dashboardVM.UserName = user.Firstname + " " + user.Lastname;
            dashboardVM.Requests = _context.Requests.Where(req => req.Userid == user.Userid).ToList();
            List<int> fileCounts = new List<int>();

            foreach (var request in dashboardVM.Requests)
            {
                int count = _context.Requestwisefiles.Count(reqFile => reqFile.Requestid == request.Requestid);
                fileCounts.Add(count);
            }
            dashboardVM.DocumentCount = fileCounts;

            return dashboardVM;
        }
    }
}
