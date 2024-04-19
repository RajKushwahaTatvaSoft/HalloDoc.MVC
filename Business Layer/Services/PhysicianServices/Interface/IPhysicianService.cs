using Business_Layer.Services.AdminProvider.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.PhysicianServices.Interface
{
    public interface IPhysicianService
    {
        public IAdminProviderService AdminProviderService { get; }
        public IPhysicianDashboardService PhysicianDashboardService { get; }
    }
}
