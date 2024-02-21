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
    }
}
