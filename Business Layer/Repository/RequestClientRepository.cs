using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;

namespace Business_Layer.Repository
{
    public class RequestClientRepository : Repository<Requestclient>, IRequestClientRepository
    {
        private ApplicationDbContext _context;
        public RequestClientRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
