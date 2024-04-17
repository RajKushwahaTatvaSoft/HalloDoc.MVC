using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class ProviderOnCallViewModel
    {
        public string? LoggedInUserName { get; set; }
        public List<DataModels.Physician>? physiciansOffDuty { get; set; }
        public List<DataModels.Physician>? physiciansOnCall { get; set; }
        public IEnumerable<Region> regions { get; set; }

    }
}
