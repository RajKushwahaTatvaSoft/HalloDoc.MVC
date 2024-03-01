using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public List<AdminRequest> adminRequests {  get; set; }
        public IEnumerable<Casetag> casetags { get; set; }
        public IEnumerable<Region> regions { get; set; }
        public IEnumerable<Physician> physicians { get; set; }
        public DashboardFilter filterOptions { get; set; }
        public string UserName { get; set; }
        public int DashboardStatus {  get; set; }
        public int NewReqCount { get; set; }
        public int PendingReqCount { get; set; }
        public int ActiveReqCount { get; set; }
        public int ConcludeReqCount { get; set; }
        public int ToCloseReqCount { get; set; }
        public int UnpaidReqCount { get; set; }
        public int CurrentPage {  get; set; }
    }
}
