using Assignment.MVC.BusinessLayer.Repository.IRepository;
using Assignment.MVC.DataLayer.DataContext;
using Assignment.MVC.DataLayer.DataModels;

namespace Assignment.MVC.BusinessLayer.Repository
{
    internal class PatientRepository : GenericRepository<Patient>, IPatientRepository
    {
        private readonly ApplicationDbContext _context;
        public PatientRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
