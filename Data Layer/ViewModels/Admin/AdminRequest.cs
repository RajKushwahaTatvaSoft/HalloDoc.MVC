using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminRequest
    {
        public int? RequestId { get; set; }
        public string PatientName { get; set; }
        public int RequestType { get; set; }
        public string DateOfBirth { get; set; }
        public string Requestor { get; set; }
        public string RequestDate { get; set; }
        public string RegionName { get; set; } = "Region Name";
        public string PhysicianName { get; set; } = "Physician Name";
        public string DateOfService { get; set; } = "Date Of Service";
        public string PatientPhone { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public string ProviderPhone { get; set; }
        public string ProviderMail { get; set; }
    }
}
