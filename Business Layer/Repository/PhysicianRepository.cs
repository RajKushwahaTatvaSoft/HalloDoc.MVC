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
    public class PhysicianRepository : GenericRepository<Physician>, IPhysicianRepository
    {
        private ApplicationDbContext _context;
        public PhysicianRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public IEnumerable<Physician> GetPhysiciansByPhysicianRegion(int regionId)
        {
            IEnumerable<int> phyRegions = _context.Physicianregions.Where(phyReg => phyReg.Regionid == regionId).Select(_ => _.Physicianid);

            IEnumerable<Physician> physicians = _context.Physicians.Where(phy => phyRegions.Contains(phy.Physicianid));

            return physicians;
        }


        public override Physician? GetFirstOrDefault(Expression<Func<Physician, bool>> filter)
        {
            IQueryable<Physician> query = dbSet.Where(admin => admin.Isdeleted != true);
            return query.FirstOrDefault(filter);
        }

        public override IQueryable<Physician> GetAll()
        {
            IQueryable<Physician> query = dbSet.Where(role => role.Isdeleted != true);
            return query;
        }

        public override IQueryable<Physician> Where(Expression<Func<Physician, bool>> filter)
        {
            IQueryable<Physician> query = dbSet.Where(role => role.Isdeleted != true);
            return query.Where(filter);
        }
    }
}
