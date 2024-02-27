using Business_Layer.Interface;
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
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}