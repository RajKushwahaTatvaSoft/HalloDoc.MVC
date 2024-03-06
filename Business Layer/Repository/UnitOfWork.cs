﻿using Business_Layer.Interface;
using Data_Layer.DataContext;

namespace Business_Layer.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _context;
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            AspNetUserRepository = new AspNetUserRepository(_context);
            UserRepository = new UserRepository(_context);
            RequestRepository = new RequestRepository(_context);
            RequestClientRepository = new RequestClientRepository(_context);
            RequestWiseFileRepository = new RequestWiseFileRepository(_context);
            ConciergeRepository = new ConciergeRepository(_context);
            RequestConciergeRepository = new RequestConciergeRepo(_context);
            RequestBusinessRepo = new RequestBusinessRepo(_context);
            BusinessRepo = new BusinessRepo(_context);
            CaseTagRepository = new CaseTagRepository(_context);
            RequestStatusLogRepository = new RequestStatusLogRepo(_context);
            PassTokenRepository = new PassTokenRepository(_context);
        }

        public IAspNetUserRepository AspNetUserRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }
        public IRequestRepository RequestRepository { get; private set; }
        public IRequestClientRepository RequestClientRepository { get; private set; }
        public IRequestWiseFileRepository RequestWiseFileRepository { get; private set; }
        public IRequestConciergeRepo RequestConciergeRepository { get; private set; }
        public IConciergeRepository ConciergeRepository { get; private set; }
        public IBusinessRepo BusinessRepo { get; private set; }
        public IRequestBusinessRepo RequestBusinessRepo { get; private set; }
        public ICaseTagRepository CaseTagRepository { get; private set; }
        public IRequestStatusLogRepo RequestStatusLogRepository { get; private set; }
        public IPassTokenRepository PassTokenRepository { get; private set; }
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}