using Business_Layer.Repository.IRepository;
using Data_Layer.DataContext;

namespace Business_Layer.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _context;
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            AdminRepository = new AdminRepository(_context);
            AspNetUserRepository = new AspNetUserRepository(_context);
            UserRepository = new UserRepository(_context);
            RequestRepository = new RequestRepository(_context);
            RequestClientRepository = new RequestClientRepository(_context);
            RequestWiseFileRepository = new RequestWiseFileRepository(_context);
            ConciergeRepository = new ConciergeRepository(_context);
            RequestConciergeRepository = new RequestConciergeRepo(_context);
            RequestBusinessRepo = new RequestBusinessRepo(_context);
            BusinessRepo = new BusinessRepo(_context);
            CaseTagRepository = new CaseTagRepository(_context);
            RequestStatusLogRepository = new RequestStatusLogRepo(_context);
            PassTokenRepository = new PassTokenRepository(_context);
            RequestNoteRepository = new RequestNoteRepository(_context);
            RegionRepository = new RegionRepository(_context);
            PhysicianRepository = new PhysicianRepository(_context);
            HealthProfessionalRepo = new HealthProfessionalRepo(_context);
            HealthProfessionalTypeRepo = new HealthProfessionalTypeRepo(_context);
            BlockRequestRepo = new BlockRequestRepo(_context);
            OrderDetailRepo = new OrderDetailRepo(_context);
            EncounterFormRepository = new EncounterFormRepository(_context);
            AdminRegionRepo = new AdminRegionRepo(_context);
            RoleRepo = new RoleRepository(_context);
            PhysicianRegionRepo = new PhysicianRegionRepository(_context);
            PhysicianLocationRepo = new PhysicianLocationRepo(_context);
            CityRepository = new CityRepository(_context);
            EmailLogRepository = new EmailLogRepository(_context);
            SMSLogRepository = new SMSLogRepository(_context);
            ShiftRepository = new ShiftRepository(_context);
            ShiftDetailRepository = new ShiftDetailRepository(_context);
            ShiftDetailRegionRepository = new ShiftDetailRegionRepo(_context);
            PhysicianNotificationRepo = new PhysicianNotificationRepository(_context);
            RoleMenuRepository = new RoleMenuRepository(_context);
            MenuRepository = new MenuRepository(_context);
            AspNetRoleRepository = new AspNetRoleRepository(_context);
            RequestStatusRepository = new RequestStatusRepository(_context);
            RequestTypeRepository = new RequestTypeRepository(_context);
            TimeSheetDetailRepo = new TimeSheetDetailRepo(_context);
            TimeSheetRepository = new TimeSheetRepository(_context);
            TimeSheetDetailReimbursementRepo = new TimeSheetDetailReimbursementRepo(_context);
            PayrateCategoryRepository = new PayrateCategoryRepository(_context);
            ProviderPayrateRepository = new ProviderPayrateRepository(_context);
            ChatMessageRepository = new ChatMessageRepository(_context);
        }

        public IAdminRepository AdminRepository { get; private set; }
        public IAspNetUserRepository AspNetUserRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }
        public IRequestRepository RequestRepository { get; private set; }
        public IRequestClientRepository RequestClientRepository { get; private set; }
        public IRequestWiseFileRepository RequestWiseFileRepository { get; private set; }
        public IRequestConciergeRepo RequestConciergeRepository { get; private set; }
        public IConciergeRepository ConciergeRepository { get; private set; }
        public IBusinessRepo BusinessRepo { get; private set; }
        public IRequestBusinessRepo RequestBusinessRepo { get; private set; }
        public ICaseTagRepository CaseTagRepository { get; private set; }
        public IRequestStatusLogRepo RequestStatusLogRepository { get; private set; }
        public IPassTokenRepository PassTokenRepository { get; private set; }
        public IRequestNoteRepository RequestNoteRepository { get; private set; }
        public IRegionRepository RegionRepository { get; private set; }
        public IPhysicianRepository PhysicianRepository { get; private set; }
        public IHealthProfessionalRepo HealthProfessionalRepo { get; private set; }
        public IHealthProfessionalTypeRepo HealthProfessionalTypeRepo { get; private set; }
        public IBlockRequestRepo BlockRequestRepo { get; private set; }
        public IOrderDetailRepo OrderDetailRepo { get; private set; }
        public IEncounterFormRepository EncounterFormRepository { get; private set; }
        public IAdminRegionRepo AdminRegionRepo { get; private set; }
        public IPhysicianRegionRepo PhysicianRegionRepo { get; private set; }
        public IRoleRepository RoleRepo { get; private set; }
        public IPhysicianLocationRepo PhysicianLocationRepo { get; private set; }
        public ICityRepository CityRepository { get; private set; }

        public IEmailLogRepository EmailLogRepository {  get; private set; }

        public ISMSLogRepository SMSLogRepository { get; private set; }

        public IShiftRepository ShiftRepository { get; private set; }

        public IShiftDetailRepository ShiftDetailRepository { get; private set; }

        public IShiftDetailRegionRepository ShiftDetailRegionRepository { get; private set; }

        public IPhysicianNotificationRepo PhysicianNotificationRepo { get; private set; }

        public IRoleMenuRepository RoleMenuRepository { get; private set; }

        public IMenuRepository MenuRepository { get; private set; }

        public IAspNetRoleRepository AspNetRoleRepository { get; private set; }

        public IRequestStatusRepository RequestStatusRepository { get; private set; }

        public IRequestTypeRepository RequestTypeRepository { get; private set; }
        public ITimeSheetRepository TimeSheetRepository { get; private set; }
        public ITimeSheetDetailRepo TimeSheetDetailRepo { get; private set; }
        public ITimeSheetDetailReimbursementRepo TimeSheetDetailReimbursementRepo { get; private set; }
        public IProviderPayrateRepository ProviderPayrateRepository { get; private set; }
        public IPayrateCategoryRepository PayrateCategoryRepository { get; private set; }
        public IChatMessageRepository ChatMessageRepository { get; private set; }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}