using Assignment.MVC.BusinessLayer.Repository.IRepository;
using Assignment.MVC.DataLayer.DataContext;

namespace Assignment.MVC.BusinessLayer.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _context;
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            PatientRepository = new PatientRepository(_context);
            DoctorRepository = new DoctorRepository(_context);
        }

        public IPatientRepository PatientRepository { get; private set; }
        public IDoctorRepository DoctorRepository { get; private set; }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
