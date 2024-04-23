using Business_Layer.Services.AdminProvider.Interface;

namespace Business_Layer.Services.AdminServices.Interface
{
    public interface IAdminService
    {
        public IAdminProviderService AdminProviderService { get; }
        public IAdminDashboardService AdminDashboardService { get; }
        public IProviderLocationService ProviderLocationService { get; }
        public IAdminProfileService AdminProfileService { get; }
        public IAdminRecordService AdminRecordService { get; }
    }
}