using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Data_Layer.DataModels;


namespace Business_Layer.Repository
{
    public class ConciergeRepository : GenericRepository<Concierge>, IConciergeRepository
    {
        private ApplicationDbContext _context;
        public ConciergeRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
