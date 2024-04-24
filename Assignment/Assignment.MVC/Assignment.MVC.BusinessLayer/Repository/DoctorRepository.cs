using Assignment.MVC.BusinessLayer.Repository.IRepository;
using Assignment.MVC.DataLayer.DataContext;
using Assignment.MVC.DataLayer.DataModels;

namespace Assignment.MVC.BusinessLayer.Repository
{
    internal class DoctorRepository : GenericRepository<Doctor> , IDoctorRepository
    {
        private readonly ApplicationDbContext _context;
        public DoctorRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
