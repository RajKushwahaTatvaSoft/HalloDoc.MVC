using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.Filter
{
    public class LogFilter
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int RoleId{ get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public DateTime? SentDate { get; set; }
    }
}
