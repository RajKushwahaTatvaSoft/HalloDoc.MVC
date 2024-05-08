using Business_Layer.Repository.IRepository;
using Business_Layer.Utilities;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System.Linq.Expressions;

namespace Business_Layer.Repository
{
    public class AspNetUserRepository : GenericRepository<Aspnetuser>, IAspNetUserRepository
    {
        private ApplicationDbContext _context;
        public AspNetUserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public bool IsUserWithEmailExists(string email)
        {
            bool isUserExists = _context.Aspnetusers.Any(u => u.Email == email);
            return isUserExists;
        }

        public bool CanPatientWithEmailCreateRequest(string email)
        {
            Aspnetuser? aspnetuser = _context.Aspnetusers.FirstOrDefault(user => user.Email != null && user.Email.ToLower().Equals(email.ToLower()));

            if (aspnetuser == null || aspnetuser.Accounttypeid == (int)AccountType.Patient)
            {
                return true;
            }

            return false;
        }

    }
}