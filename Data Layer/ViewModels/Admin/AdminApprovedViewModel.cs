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
        public IEnumerable<ProviderPayrate> providerPayrates {  get; set; }
        public TimeSheetFormViewModel TimesheetDetails {  get; set; }
        public decimal BonusAmount { get; set; }
        public string? AdminDescription {  get; set; }

    }
}