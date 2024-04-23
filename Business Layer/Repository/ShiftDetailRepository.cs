using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    public class ShiftDetailRepository : GenericRepository<Shiftdetail>, IShiftDetailRepository
    {
        private ApplicationDbContext _context;
        public ShiftDetailRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override Shiftdetail? GetFirstOrDefault(Expression<Func<Shiftdetail, bool>> filter)
        {
            IQueryable<Shiftdetail> query = dbSet.Where(shift => shift.Isdeleted != true);
            return query.FirstOrDefault(filter);
        }

        public override IQueryable<Shiftdetail> GetAll()
        {
            IQueryable<Shiftdetail> query = dbSet.Where(shift => shift.Isdeleted != true);
            return query;
        }

        public override IQueryable<Shiftdetail> Where(Expression<Func<Shiftdetail, bool>> filter)
        {
            IQueryable<Shiftdetail> query = dbSet.Where(shift => shift.Isdeleted != true);
            return query.Where(filter);
        }

        public bool IsAnotherShiftExists(int physicianId, DateTime shiftDate, TimeOnly? startTime, TimeOnly? endTime)
        {
            bool isExists = _context.Shiftdetails.Any(
                    sd => sd.Isdeleted != true
                    && sd.Shift.Physicianid == physicianId
                    && sd.Shiftdate.Date == shiftDate
                && ((startTime <= sd.Endtime && startTime >= sd.Starttime) // checks if new start time falls between exisiting
                    || (endTime >= sd.Starttime && endTime <= sd.Endtime)  // checks if new end time falls between existing)
                    || (sd.Starttime <= endTime && sd.Starttime >= startTime) // checks if exists start time falls between new 
                    || (sd.Endtime >= startTime && sd.Endtime <= endTime)) // checks if exists end time falls between new
                );

            return isExists;

        }
    }
}
