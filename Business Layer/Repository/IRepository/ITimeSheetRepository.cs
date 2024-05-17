using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.IRepository
{
    public interface ITimeSheetRepository : IGenericRepository<Timesheet>
    {
        public decimal GetInvoiceTotal(int timeSheetId);
    }
}
