using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Utilities
{
    public static class DateHelper
    {
        public static DateTime GetDOBDateTime(int year, string month, int date)
        {

            string dobDate = year.ToString("D4") + "-" + Convert.ToInt32(month).ToString("D2") + "-" + date.ToString("D2");
            return DateTime.Parse(dobDate);
        }
    }
}
