using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Admin
{
    public class UserAccessTRow
    {
        public string AccountType { get; set; }
        public int AccountTypeId { get; set; }
        public string AccountPOC { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public string OpenRequests { get; set; }
        public string AspnetUserId { get; set; }
    }
}
