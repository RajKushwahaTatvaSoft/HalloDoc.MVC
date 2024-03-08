using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    public class PhysicianRepository : Repository<Physician>, IPhysicianRepository
    {
        private ApplicationDbContext _context;
        public PhysicianRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
