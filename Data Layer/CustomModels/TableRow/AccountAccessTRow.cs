using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow
{
    public class AccountAccessTRow
    {
        public int? Id { get; set; }
        public int? AccountType { get; set; }
        public string? Name { get; set; }
        public string? AccounttypeName { get; set; }
        public int? OpenRequests { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
