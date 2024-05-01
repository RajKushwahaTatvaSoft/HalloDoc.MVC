using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Physician
{
    public class TimeSheetDayRecord
    {
        public DateTime DateOfRecord {  get; set; }
        public int OnCallHours {  get; set; }
        public int TotalHours {  get; set; }
        public bool IsHoliday {  get; set; }
        public int NoOfHouseCall {  get; set; }
        public int NoOfPhoneConsult {  get; set; }
    }
}