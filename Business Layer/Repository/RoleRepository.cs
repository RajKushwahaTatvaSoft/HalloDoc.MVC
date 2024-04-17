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
    internal class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        private ApplicationDbContext _context;
        public RoleRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public override Role? GetFirstOrDefault(Expression<Func<Role, bool>> filter)
        {
            IQueryable<Role> query = dbSet.Where(role => role.Isdeleted != true);
            return query.FirstOrDefault(filter);
        }

        public override IQueryable<Role> GetAll()
        {
            IQueryable<Role> query = dbSet.Where(role => role.Isdeleted != true);            
            return query;
        }

        public override IQueryable<Role> Where(Expression<Func<Role, bool>> filter)
        {
            IQueryable<Role> query = dbSet.Where(role=> role.Isdeleted != true);
            return query.Where(filter);
        }
    }
}