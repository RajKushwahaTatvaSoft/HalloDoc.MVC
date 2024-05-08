using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Patient
{
    public class PatientDashboardTRow
    {
        public int RequestId { get; set; }
        public string RequestStatus { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
