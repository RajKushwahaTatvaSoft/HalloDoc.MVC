using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Business_Layer.Repository.IRepository;

namespace Business_Layer.Repository
{
    public class RequestRepository : GenericRepository<Request>, IRequestRepository
    {
        private ApplicationDbContext _context;
        public RequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
