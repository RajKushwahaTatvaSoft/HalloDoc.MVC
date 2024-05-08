using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.IRepository
{
    public interface IAspNetUserRepository : IGenericRepository<Aspnetuser>
    {
        public bool IsUserWithEmailExists(string email);
        public bool CanPatientWithEmailCreateRequest(string email);
    }
}
