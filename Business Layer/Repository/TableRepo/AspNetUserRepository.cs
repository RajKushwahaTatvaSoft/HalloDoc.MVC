using Business_Layer.Interface.TableInterface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;

namespace Business_Layer.Repository.TableRepo
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