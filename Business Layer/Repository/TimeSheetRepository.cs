using Business_Layer.Repository.IRepository;
using Business_Layer.Utilities;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    public class TimeSheetRepository : GenericRepository<Timesheet>, ITimeSheetRepository
    {
        private ApplicationDbContext _context;
        public TimeSheetRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public decimal GetInvoiceTotal(int timeSheetId)
        {
            try
            {
                Timesheet? timesheet = _context.Timesheets.FirstOrDefault(sheet => sheet.TimesheetId == timeSheetId);
                if (timesheet == null)
                {
                    return 0;
                }
                
                Physician? phy = _context.Physicians.FirstOrDefault(phy=> phy.Physicianid == timesheet.PhysicianId);

                if (phy == null)
                {
                    return 0;
                }

                //IEnumerable<TimesheetDetail> list = timesheet.TimesheetDetails;
                //IEnumerable<ProviderPayrate> payrateList = phy.ProviderPayrates;
                IEnumerable<TimesheetDetail> list = _context.TimesheetDetails.Where(sheet => sheet.TimesheetId == timeSheetId);
                IEnumerable<ProviderPayrate> payrateList = _context.ProviderPayrates.Where(payrate => payrate.PhysicianId == phy.Physicianid);

                double totalHours = 0;
                int noOfNightWeekend = 0;
                int noOfHouseCalls = 0;
                int noOfPhoneConsults = 0;

                foreach (TimesheetDetail sheetDetail in list)
                {
                    if (sheetDetail.IsWeekend ?? false)
                    {
                        noOfNightWeekend++;
                    }

                    totalHours += sheetDetail.TotalHours ?? 0;
                    noOfHouseCalls += sheetDetail.NumberOfHouseCall ?? 0;
                    noOfPhoneConsults += sheetDetail.NumberOfPhoneCall ?? 0;
                }


                decimal shiftPayrate = payrateList.FirstOrDefault(rate => rate.PayrateCategoryId == (int)PayrateCategoryEnum.Shift)?.Payrate ?? 0;
                decimal nightShiftWeekend = payrateList.FirstOrDefault(rate => rate.PayrateCategoryId == (int)PayrateCategoryEnum.NightShiftWeekend)?.Payrate ?? 0;
                decimal houseCallPayrate = payrateList.FirstOrDefault(rate => rate.PayrateCategoryId == (int)PayrateCategoryEnum.HouseCall)?.Payrate ?? 0;
                decimal phoneConsultPayrate = payrateList.FirstOrDefault(rate => rate.PayrateCategoryId == (int)PayrateCategoryEnum.PhoneConsult)?.Payrate ?? 0;

                decimal totalHoursInvoice = Convert.ToDecimal(totalHours) * shiftPayrate;
                decimal nightWeekendInvoice = noOfNightWeekend * nightShiftWeekend;
                decimal houseCallsInvoice = noOfHouseCalls * houseCallPayrate;
                decimal phoneConsultsInvoice = noOfPhoneConsults * phoneConsultPayrate;

                decimal totalInvoice = totalHoursInvoice + nightWeekendInvoice + houseCallsInvoice + phoneConsultsInvoice;

                return totalInvoice;
            }
            catch
            {
                return 0;
            }
        }
    }
}
