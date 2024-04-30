using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
