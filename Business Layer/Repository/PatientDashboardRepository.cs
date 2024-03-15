using Business_Layer.Interface;
using Business_Layer.Repository.AdminRepo;
using Data_Layer.CustomModels;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
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
                             RequestStatus = GetStatusString(r.Status),
                             CreatedDate = r.Createddate,
                             FileCount = _context.Requestwisefiles.Count(file => file.Requestid == r.Requestid),
                         }).AsQueryable();

            return await PagedList<PatientDashboardRequest>.CreateAsync(
            query, pageNumber, pageSize);

        }

        public static string GetStatusString(int status)
        {
            switch (status)
            {
                case (int)RequestStatus.Unassigned:
                    return "Unassigned";
                case (int)RequestStatus.Accepted:
                    return "Accepted";
                case (int)RequestStatus.Cancelled:
                    return "Cancelled";
                case (int)RequestStatus.MDEnRoute:
                    return "MDEnRoute";
                case (int)RequestStatus.MDOnSite:
                    return "MDOnSite";
                case (int)RequestStatus.Conclude:
                    return "Conclude";
                case (int)RequestStatus.CancelledByPatient:
                    return "CancelledByPatient";
                case (int)RequestStatus.Closed:
                    return "Closed";
                case (int)RequestStatus.Unpaid:
                    return "Unpaid";
                case (int)RequestStatus.Clear:
                    return "Clear";
                case (int)RequestStatus.Block:
                    return "Block";
            }

            return null;
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
