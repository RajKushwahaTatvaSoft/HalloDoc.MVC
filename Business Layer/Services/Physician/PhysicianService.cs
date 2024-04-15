using Business_Layer.Repository.IRepository;
using Business_Layer.Services.AdminProvider;
using Business_Layer.Services.AdminProvider.Interface;
using Business_Layer.Services.Helper.Interface;
using Business_Layer.Services.Physician.Interface;

namespace Business_Layer.Services.Physician
{
    public class PhysicianService : IPhysicianService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IUtilityService _utilityService;
        public PhysicianService(IUnitOfWork unitOfWork,IUtilityService utilityService, IEmailService emailService) {
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _emailService = emailService;
            AdminProviderService = new AdminProviderService(_unitOfWork,_utilityService,_emailService);
            PhysicianDashboardService = new PhysicianDashboardService(_unitOfWork);
        }

        public IAdminProviderService AdminProviderService { get; private set; }
        public IPhysicianDashboardService PhysicianDashboardService { get; private set; }
    }
}
