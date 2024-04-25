using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminProvider;
using Business_Layer.Services.AdminProvider.Interface;
using Business_Layer.Services.AdminServices.Interface;
using Business_Layer.Services.Helper.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Services.AdminServices
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IUtilityService _utilityService;
        public AdminService(IUnitOfWork unitOfWork, IConfiguration config,IEmailService emailService,IUtilityService utilityService)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _emailService = emailService;
            _utilityService = utilityService;
            AdminProviderService = new AdminProviderService(_unitOfWork, _utilityService, _emailService);
            AdminDashboardService = new AdminDashboardService(_unitOfWork);
            ProviderLocationService = new ProviderLocationService(_config, _unitOfWork);
            AdminProfileService = new AdminProfileService(_unitOfWork);
            AdminRecordService = new AdminRecordService(_unitOfWork);
            AdminAccessService = new AdminAccessService(_unitOfWork);
        }

        public IAdminProviderService AdminProviderService { get; private set; }
        public IAdminDashboardService AdminDashboardService { get; private set; }
        public IProviderLocationService ProviderLocationService { get; private set; }
        public IAdminProfileService AdminProfileService { get; private set; }
        public IAdminRecordService AdminRecordService { get; private set; }
        public IAdminAccessService AdminAccessService { get; private set; }
    }
}
