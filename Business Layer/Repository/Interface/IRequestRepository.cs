using Data_Layer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business_Layer.Repository.Interface
{
    public interface IRequestRepository
    {
        public void AddPatientRequest(PatientRequestViewModel prvm);
        public void AddFamilyFriendRequest(FamilyFriendRequestViewModel frvm);
        public void AddConciergeRequest(ConciergeRequestViewModel crvm);
        public void AddBusinessRequest(BusinessRequestViewModel brvm);
    }
}
