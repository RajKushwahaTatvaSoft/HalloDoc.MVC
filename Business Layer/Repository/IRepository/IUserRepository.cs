using Data_Layer.DataModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.IRepository
{
    public interface IUserRepository : IGenericRepository<User>
    {
        public User GetUserWithID(int userId);
        public User GetUserWithEmail(string email);
        public bool IsUserWithEmailExists(string email);


    }
}
