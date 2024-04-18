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
    public class AdminRepository : GenericRepository<Admin>, IAdminRepository
    {
        private ApplicationDbContext _context;
        public AdminRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override Admin? GetFirstOrDefault(Expression<Func<Admin, bool>> filter)
        {
            IQueryable<Admin> query = dbSet.Where(admin => admin.Isdeleted != true);
            return query.FirstOrDefault(filter);
        }

        public override IQueryable<Admin> GetAll()
        {
            IQueryable<Admin> query = dbSet.Where(role => role.Isdeleted != true);
            return query;
        }

        public override IQueryable<Admin> Where(Expression<Func<Admin, bool>> filter)
        {
            IQueryable<Admin> query = dbSet.Where(role => role.Isdeleted != true);
            return query.Where(filter);
        }
    }
}
