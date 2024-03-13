using Business_Layer.Interface.TableInterface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;

namespace Business_Layer.Repository.TableRepo
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
