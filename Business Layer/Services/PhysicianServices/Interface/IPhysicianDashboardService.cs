using Data_Layer.CustomModels;
using Data_Layer.CustomModels.Filter;
using Data_Layer.CustomModels.TableRow.Physician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.PhysicianServices.Interface
{
    public interface IPhysicianDashboardService
    {
        public Task<PagedList<PhyDashboardTRow>> GetPhysicianRequestAsync(DashboardFilter dashboardParams, int physicianId);

        public List<AdminRequest> GetAllRequestByStatus(int status);
    }
}
