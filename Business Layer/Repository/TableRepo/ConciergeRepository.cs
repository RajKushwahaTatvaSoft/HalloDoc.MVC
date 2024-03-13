using Business_Layer.Interface.TableInterface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;


namespace Business_Layer.Repository.TableRepo
{
    public class ConciergeRepository : Repository<Concierge>, IConciergeRepository
    {
        private ApplicationDbContext _context;
        public ConciergeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
