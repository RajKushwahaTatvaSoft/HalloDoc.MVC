using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Admin
{
    public class VendorTRow
    {
        public int BusinessId { get; set; }
        public int ProfessionId { get; set; }
        public string ProfessionName { get; set; }
        public string BusinessName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FaxNumber { get; set; }
        public string BusinessContact { get; set; }

    }
}
