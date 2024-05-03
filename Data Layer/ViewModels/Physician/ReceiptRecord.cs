using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Physician
{
    public class ReceiptRecord
    {
        public int RecordId {  get; set; }
        public DateOnly DateOfRecord {  get; set; }
        public string? ItemName {  get; set; }
        public int Amount { get; set; }
        public IFormFile? BillReceipt { get; set; }

    }
}
