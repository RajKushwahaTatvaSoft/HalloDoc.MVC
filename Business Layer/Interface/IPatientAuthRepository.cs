using Data_Layer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Interface
{
    public interface IPatientAuthRepository
    {
        public void CreateUserAccount(ForgotPasswordViewModel fpvm);
    }
}
