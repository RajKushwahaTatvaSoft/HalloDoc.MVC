using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Data_Layer.DataModels;

namespace Business_Layer.Repository
{
    public class RequestClientRepository : GenericRepository<Requestclient>, IRequestClientRepository
    {
        private ApplicationDbContext _context;
        public RequestClientRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
