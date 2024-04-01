using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Interface.Services
{
    public interface IUtilityService
    {
        public IEnumerable<City> GetCitiesByRegion(int regionId);
    }
}
