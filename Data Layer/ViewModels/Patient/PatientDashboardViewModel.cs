using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class PatientDashboardViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public List<Request> Requests { get; set; }
        public List<int> DocumentCount { get; set; }
    }
}
