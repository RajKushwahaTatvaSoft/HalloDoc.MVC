using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.IRepository
{
    public interface IPassTokenRepository : IGenericRepository<Passtoken>
    {
        public bool ValidatePassToken(string token, bool isResetToken, out string errorMessage);
    }
}
