using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.Filter
{
    public class SearchRecordFilter
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int RequestStatus { get; set; }
        public int RequestType { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? FromDateOfService { get; set; }
        public DateTime? ToDateOfService { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
    }
}
