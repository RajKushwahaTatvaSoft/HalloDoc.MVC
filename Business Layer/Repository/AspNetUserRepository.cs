using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;

namespace Business_Layer.Repository
{
    public class AspNetUserRepository : Repository<Aspnetuser>, IAspNetUserRepository
    {
        private ApplicationDbContext _context;
        public AspNetUserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}