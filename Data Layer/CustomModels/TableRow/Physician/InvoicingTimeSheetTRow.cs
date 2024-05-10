﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Physician
{
    public class InvoicingTimeSheetTRow
    {
        public int TimeSheetDetailId { get; set; }
        public DateOnly ShiftDate { get; set; }
        public double ShiftCount { get; set; }
        public int NightShiftsWeekendCount { get; set; }
        public int HouseCall { get; set; }
        public int HouseCallNightWeekendCount { get; set; }
        public int PhoneConsults { get; set; }
        public int PhoneConsultsNightWeekendCount { get; set; }
        public int BatchTesting { get; set; }

    }
}
