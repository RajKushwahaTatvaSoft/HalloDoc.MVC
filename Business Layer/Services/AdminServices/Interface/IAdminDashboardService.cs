using Data_Layer.CustomModels;
using Data_Layer.CustomModels.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminServices.Interface
{
    public interface IAdminDashboardService
    {
        public Task<PagedList<AdminRequest>> GetAdminRequestsAsync(DashboardFilter dashboardParams);

        public List<AdminRequest> GetAllRequestByStatus(int status);
    }
}
