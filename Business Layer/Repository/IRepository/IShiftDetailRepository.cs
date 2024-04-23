using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.IRepository
{
    public interface IShiftDetailRepository : IGenericRepository<Shiftdetail>
    {
        public bool IsAnotherShiftExists(int physicianId, DateTime shiftDate, TimeOnly? startTime, TimeOnly? endTime);
    }
}
