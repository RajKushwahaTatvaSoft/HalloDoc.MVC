using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Business_Layer.Repository.IRepository;
using System.Linq.Expressions;

namespace Business_Layer.Repository
{
    public class RequestRepository : GenericRepository<Request>, IRequestRepository
    {
        private ApplicationDbContext _context;
        public RequestRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override Request? GetFirstOrDefault(Expression<Func<Request, bool>> filter)
        {
            IQueryable<Request> query = dbSet.Where(admin => admin.Isdeleted != true);
            return query.FirstOrDefault(filter);
        }

        public override IQueryable<Request> GetAll()
        {
            IQueryable<Request> query = dbSet.Where(role => role.Isdeleted != true);
            return query;
        }

        public override IQueryable<Request> Where(Expression<Func<Request, bool>> filter)
        {
            IQueryable<Request> query = dbSet.Where(role => role.Isdeleted != true);
            return query.Where(filter);
        }
    }
}
