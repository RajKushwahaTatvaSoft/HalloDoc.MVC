using Assignment.MVC.BusinessLayer.Repository.IRepository;
using Assignment.MVC.DataLayer.DataContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Assignment.MVC.BusinessLayer.Repository
{
    internal class GenericRepository<T> : IGenericRepository<T> where T : class
    {

        private readonly ApplicationDbContext _context;
        internal DbSet<T> dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            dbSet = _context.Set<T>();
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public void Update(T entity)
        {
            dbSet.Update(entity);
        }
        public virtual IQueryable<T> GetAll()
        {
            IQueryable<T> query = dbSet;
            return query;
        }

        public virtual IQueryable<T> Where(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = dbSet;
            return query.Where(filter);
        }
        public virtual T? GetFirstOrDefault(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = dbSet;
            return query.FirstOrDefault(filter);
        }
    }
}
