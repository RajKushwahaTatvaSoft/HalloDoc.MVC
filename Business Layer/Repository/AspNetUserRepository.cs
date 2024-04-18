using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System.Linq.Expressions;

namespace Business_Layer.Repository
{
    public class AspNetUserRepository : GenericRepository<Aspnetuser>, IAspNetUserRepository
    {
        private ApplicationDbContext _context;
        public AspNetUserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

    }
}