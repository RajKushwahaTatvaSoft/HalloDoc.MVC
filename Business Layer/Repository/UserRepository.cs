using Business_Layer.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public bool IsUserWithEmailExists(string email)
        {
            bool isUserExists = _context.Aspnetusers.Any(u => u.Email == email);

            return isUserExists;

        }
        public User GetUserWithID(int userid)
        {
            User user = _context.Users.FirstOrDefault(u => u.Userid == userid);
            return user;
        }

        public User GetUserWithEmail(string email)
        {
            User user = _context.Users.FirstOrDefault(u => u.Email == email);
            return user;

        }
    }
}
