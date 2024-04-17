using Data_Layer.CustomModels;
using Data_Layer.ViewModels.Guest;

namespace Business_Layer.Services.Guest.Interface
{
    public interface IRequestService
    {
        public ServiceResponse SubmitPatientRequest(PatientRequestViewModel model);
        public ServiceResponse SubmitFamilyFriendRequest(FamilyFriendRequestViewModel model,string link);
        public ServiceResponse SubmitConciergeRequest(ConciergeRequestViewModel model,string link);
        public ServiceResponse SubmitBusinessRequest(BusinessRequestViewModel model, string createAccLink);
    }
}