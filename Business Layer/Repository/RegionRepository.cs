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
    public class RegionRepository : GenericRepository<Region>, IRegionRepository
    {
        private ApplicationDbContext _context;
        public RegionRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
