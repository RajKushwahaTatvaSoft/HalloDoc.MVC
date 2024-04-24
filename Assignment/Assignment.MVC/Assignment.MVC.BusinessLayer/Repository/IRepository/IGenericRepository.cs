using System.Linq.Expressions;

namespace Assignment.MVC.BusinessLayer.Repository.IRepository
{
    public interface IGenericRepository<T> where T : class
    {
        T? GetFirstOrDefault(Expression<Func<T, bool>> filter);
        void Add(T entity);
        void Remove(T entity);
        void Update(T entity);
        IQueryable<T> GetAll();
        public IQueryable<T> Where(Expression<Func<T, bool>> filter);

    }
}
