using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminServices.Interface;
using Data_Layer.CustomModels;
using Data_Layer.ViewModels.Admin;
using Microsoft.Extensions.Configuration;

namespace Business_Layer.Services.AdminServices
{
    public class ProviderLocationService : IProviderLocationService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;

        public ProviderLocationService(IConfiguration config, IUnitOfWork unitOfWork)
        {
            _config = config;
            _unitOfWork = unitOfWork;
        }

        public ProviderLocationViewModel? GetProviderLocationModel()
        {

            IEnumerable<PhyLocationRow> list = from pl in _unitOfWork.PhysicianLocationRepo.GetAll()
                                                select new PhyLocationRow
                                                {
                                                    PhysicianName = pl.Physicianname,
                                                    Latitude = pl.Latitude ?? 0,
                                                    Longitude = pl.Longitude ?? 0,
                                                };

            string? apiKey = _config.GetSection("TomTom")["ApiKey"];

            if (apiKey == null)
            {
                return null;
            }

            ProviderLocationViewModel model = new ProviderLocationViewModel()
            {
                locationList = list,
                ApiKey = apiKey,
            };

            return model;
        }
    }
}
