using Business_Layer.Services.Helper.Interface;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Mvc;

namespace Business_Layer.Services.Helper
{
    public class Utilities : IUtilityService
    {

        private readonly ApplicationDbContext _context;
        public Utilities(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<City> GetCitiesByRegion(int regionId)
        {
            IEnumerable<City> cities = _context.Cities.Where(city => city.Regionid == regionId).OrderBy(_ => _.Name);
            return cities;
        }

    }
}
