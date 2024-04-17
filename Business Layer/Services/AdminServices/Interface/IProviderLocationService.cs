using Data_Layer.ViewModels.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminServices.Interface
{
    public interface IProviderLocationService
    {
        public ProviderLocationViewModel? GetProviderLocationModel();
    }
}
