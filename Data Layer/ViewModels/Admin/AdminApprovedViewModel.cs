using Data_Layer.DataModels;
using Data_Layer.ViewModels.Physician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminApprovedViewModel
    {
        public TimeSheetFormViewModel TimesheetDetails {  get; set; }
        public IEnumerable<ProviderPayrate> providerPayrates {  get; set; }
    }
}