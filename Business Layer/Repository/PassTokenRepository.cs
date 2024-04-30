using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository
{
    public class PassTokenRepository : GenericRepository<Passtoken>, IPassTokenRepository
    {
        private ApplicationDbContext _context;
        public PassTokenRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public bool ValidatePassToken(string token, bool isResetToken, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (token == null)
            {
                errorMessage = "Invalid Token. Cannot Reset Password";
                return false;
            }

            Passtoken? passtoken = _context.Passtokens.FirstOrDefault(pass => pass.Uniquetoken == token);
            if (passtoken == null || passtoken.Isresettoken != isResetToken || passtoken.Isdeleted)
            {
                errorMessage = "Invalid Token. Cannot Reset Password";
                return false;
            }

            TimeSpan diff = DateTime.Now - passtoken.Createddate;
            if (diff.Hours > 24)
            {
                errorMessage = "Token Expired. Cannot Reset Password";
                return false;
            }

            return true;
        }
    }
}
