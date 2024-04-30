using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    internal class RequestWiseFileRepository : GenericRepository<Requestwisefile>, IRequestWiseFileRepository
    {
        private ApplicationDbContext _context;
        public RequestWiseFileRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override Requestwisefile? GetFirstOrDefault(Expression<Func<Requestwisefile, bool>> filter)
        {
            IQueryable<Requestwisefile> query = dbSet.Where(admin => admin.Isdeleted != true);
            return query.FirstOrDefault(filter);
        }

        public override IQueryable<Requestwisefile> GetAll()
        {
            IQueryable<Requestwisefile> query = dbSet.Where(role => role.Isdeleted != true);
            return query;
        }

        public override IQueryable<Requestwisefile> Where(Expression<Func<Requestwisefile, bool>> filter)
        {
            IQueryable<Requestwisefile> query = dbSet.Where(role => role.Isdeleted != true);
            return query.Where(filter);
        }

    }
}
