using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels
{
    public class PatientDashboardRequest
    {
        public int RequestId {  get; set; }
        public string RequestStatus { get; set; }
        public int FileCount { get; set; }
        public DateTime CreatedDate {  get; set; }
    }
}
