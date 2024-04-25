﻿using Data_Layer.CustomModels;
using Data_Layer.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminServices.Interface
{
    public interface IAdminAccessService
    {
        public ServiceResponse EditAccessSubmit(EditAccessViewModel model);
        public ServiceResponse DeleteRole(int roleId);
        public AdminProfileViewModel? GetEditAdminAccountModel(string adminAspId);
    }
}
