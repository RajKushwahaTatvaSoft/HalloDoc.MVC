using Business_Layer.Interface;
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
        public PatientDashboardRepository(ApplicationDbContext context) {
            _context = context;
        }
        public User GetUserWithID(int userid)
        {
            User user = _context.Users.FirstOrDefault(u => u.Userid == userid);
            return user;
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
