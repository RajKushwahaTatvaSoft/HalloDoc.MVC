using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Physician
{
    public class TimeSheetDayRecord
    {
        public int TimeSheetDetailId { get; set; }
        public DateOnly DateOfRecord {  get; set; }
        public double OnCallHours {  get; set; }
        public double TotalHours {  get; set; }
        public bool IsHoliday {  get; set; }
        public int NoOfHouseCall {  get; set; }
        public int NoOfPhoneConsult {  get; set; }
    }
}