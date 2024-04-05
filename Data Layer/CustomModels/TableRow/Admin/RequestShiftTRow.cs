using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Admin
{
    public class RequestShiftTRow
    {
        public int ShiftDetailId { get; set; }
        public string Staff { get; set;}
        public DateTime ShiftDate {  get; set; }
        public TimeOnly ShiftStartTime {  get; set; }
        public TimeOnly ShiftEndTime { get; set; }
        public string RegionName {  get; set; }
    }
}
