using Data_Layer.CustomModels.TableRow.Physician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Physician
{
    public class InvoicingTimeSheetViewModel
    {
        public bool IsFinalized { get; set; }
        public DateOnly StartDate {  get; set; }
        public DateOnly EndDate { get; set; }
        public IEnumerable<InvoicingTimeSheetTRow>? timeSheetRecords {  get; set; }
    }
}
