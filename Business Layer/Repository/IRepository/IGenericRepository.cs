using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.IRepository
{
    public interface IGenericRepository<T> where T : class
    {
        T? GetFirstOrDefault(Expression<Func<T, bool>> filter);
        void Add(T entity);
        void Remove(T entity);
        void Update(T entity);
        IQueryable<T> GetAll();
        public IQueryable<T> Where(Expression<Func<T, bool>> filter);
        public int Count();
        public int Count(Expression<Func<T, bool>> filter);

    }
}
