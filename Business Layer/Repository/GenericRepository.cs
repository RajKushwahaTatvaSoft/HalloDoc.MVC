﻿using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
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

        public IQueryable<T> GetAll()
        {
            IQueryable<T> query = dbSet;
            return query;
        }

        public IQueryable<T> Where(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = dbSet;
            return query.Where(filter);
        }

        public int Count()
        {
            IQueryable<T> query = dbSet;
            return query.Count();
        }

        public int Count(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = dbSet;
            return query.Count(filter);
        }

        public T? GetFirstOrDefault(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = dbSet;
            return query.FirstOrDefault(filter);
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public void Update(T entity)
        {
            dbSet.Update(entity);
        }

    }
}
