
using Business_Layer.Interface.TableInterface;

namespace Business_Layer.Interface
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
        void Save();
    }

}