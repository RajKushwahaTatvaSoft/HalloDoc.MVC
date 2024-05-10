using Data_Layer.ViewModels.Physician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class ReceiptPartialViewModel
    {
        public List<ReceiptRecord> receiptRecords = new List<ReceiptRecord>();
        public int TimeSheetId {  get; set; }
    }
}
