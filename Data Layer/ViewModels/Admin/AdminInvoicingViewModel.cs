using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminInvoicingViewModel
    {
        public IEnumerable<DataModels.Physician>? physicians { get; set; }
    }
}
