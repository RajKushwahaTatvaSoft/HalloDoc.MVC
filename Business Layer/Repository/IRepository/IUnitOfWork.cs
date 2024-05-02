namespace Business_Layer.Repository.IRepository
{
    public interface IUnitOfWork
    {
        IAdminRepository AdminRepository { get; }
        IAspNetUserRepository AspNetUserRepository { get; }
        IUserRepository UserRepository { get; }
        IRequestRepository RequestRepository { get; }
        IRequestClientRepository RequestClientRepository { get; }
        IRequestWiseFileRepository RequestWiseFileRepository { get; }
        IConciergeRepository ConciergeRepository { get; }
        IRequestConciergeRepo RequestConciergeRepository { get; }
        IBusinessRepo BusinessRepo { get; }
        IEncounterFormRepository EncounterFormRepository { get; }
        IRequestBusinessRepo RequestBusinessRepo { get; }
        ICaseTagRepository CaseTagRepository { get; }
        IRequestStatusLogRepo RequestStatusLogRepository { get; }
        IPassTokenRepository PassTokenRepository { get; }
        IRequestNoteRepository RequestNoteRepository { get; }
        IRegionRepository RegionRepository { get; }
        IPhysicianRepository PhysicianRepository { get; }
        IHealthProfessionalRepo HealthProfessionalRepo { get; }
        IHealthProfessionalTypeRepo HealthProfessionalTypeRepo { get; }
        IBlockRequestRepo BlockRequestRepo { get; }
        IOrderDetailRepo OrderDetailRepo { get; }
        IAdminRegionRepo AdminRegionRepo { get; }
        IRoleRepository RoleRepo { get; }
        IPhysicianRegionRepo PhysicianRegionRepo { get; }
        IPhysicianLocationRepo PhysicianLocationRepo { get; }
        ICityRepository CityRepository { get; }
        IEmailLogRepository EmailLogRepository { get; }
        ISMSLogRepository SMSLogRepository { get; }
        IShiftRepository ShiftRepository { get; }
        IShiftDetailRepository ShiftDetailRepository { get; }
        IShiftDetailRegionRepository ShiftDetailRegionRepository { get; }
        IPhysicianNotificationRepo PhysicianNotificationRepo { get; }
        IRoleMenuRepository RoleMenuRepository { get; }
        IMenuRepository MenuRepository { get; }
        IAspNetRoleRepository AspNetRoleRepository { get; }
        IRequestStatusRepository RequestStatusRepository { get; }
        IRequestTypeRepository RequestTypeRepository { get; }
        ITimeSheetRepository TimeSheetRepository { get; }
        ITimeSheetDetailRepo TimeSheetDetailRepo { get; }
        void Save();
    }

}