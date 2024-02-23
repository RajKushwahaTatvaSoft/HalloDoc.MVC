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
        public string UserName { get; set; }
        public int DashboardStatus {  get; set; }
        public int newReqCount { get; set; }
        public int pendingReqCount { get; set; }
        public int activeReqCount { get; set; }
        public int concludeReqCount { get; set; }
        public int toCloseReqCount { get; set; }
        public int unpaidReqCount { get; set; }

    }
}
