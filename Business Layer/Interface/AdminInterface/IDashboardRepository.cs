using Data_Layer.CustomModels;
using Data_Layer.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Interface.AdminInterface
{
    public interface IDashboardRepository
    {
        public Task<PagedList<AdminRequest>> GetAdminRequestsAsync(DashboardFilter dashboardParams);
    }
}
