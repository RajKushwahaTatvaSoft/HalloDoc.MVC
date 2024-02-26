using Data_Layer.DataModels;
using Data_Layer.ViewModels;

namespace Business_Layer.Interface
{
    public interface IRequestRepository
    {
        public void AddRequestForMe(MeRequestViewModel mrvm, string webRootPath, int userid);
        public MeRequestViewModel FetchRequestForMe(int userid);
        public void AddRequestForSomeoneElse(SomeoneElseRequestViewModel srvm, string webRootPath, int userid, bool isNewUser);
        public void AddPatientRequest(PatientRequestViewModel prvm, string webRootPath, bool isNewUser);
        public void AddFamilyFriendRequest(FamilyFriendRequestViewModel frvm, string webRootPath, bool isNewUser);
        public void AddConciergeRequest(ConciergeRequestViewModel crvm, string webRootPath, bool isNewUser);
        public void AddBusinessRequest(BusinessRequestViewModel brvm, string webRootPath, bool isNewUser);
        public bool IsUserWithGivenEmailExists(string email);
        public User GetUserWithID(int id);
    }
}
