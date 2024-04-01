using Business_Layer.Interface.Services;
using Data_Layer.DataContext;
using Data_Layer.DataModels;
using Microsoft.AspNetCore.Mvc;

namespace HalloDoc.MVC.Services
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
            IEnumerable<City> cities = _context.Cities.Where(city => city.Regionid == regionId);
            return cities;
        }

    }
}
