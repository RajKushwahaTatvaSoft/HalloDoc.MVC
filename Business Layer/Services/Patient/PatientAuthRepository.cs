using Business_Layer.Services.Patient.Interface;
using Business_Layer.Utilities;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.Patient
{
    public class PatientAuthRepository : IPatientAuthRepository
    {
        private readonly ApplicationDbContext _context;
        public PatientAuthRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void CreateUserAccount(ForgotPasswordViewModel fpvm)
        {
            Aspnetuser user = _context.Aspnetusers.FirstOrDefault(u => u.Email == fpvm.Email);

            string passHash = AuthHelper.GenerateSHA256(fpvm.Password);
            user.Passwordhash = passHash;
            user.Modifieddate = DateTime.Now;
            _context.Update(user);
            _context.SaveChanges();

        }
    }
}
