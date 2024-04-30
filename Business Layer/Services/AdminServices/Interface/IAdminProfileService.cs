using Data_Layer.CustomModels;
using Data_Layer.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminServices.Interface
{
    public interface IAdminProfileService
    {
        public AdminProfileViewModel? GetAdminProfileModel(int adminId);
        public ServiceResponse UpdateAdminPassword(int adminId, string password);
        public ServiceResponse UpdateAdminAccountInfo(int roleId,int statusId, int adminId);
        public ServiceResponse UpdateAdminPersonalDetails(int adminId, List<int>? regions, string firstName, string lastName, string email, string phone);
    }
}
