using Business_Layer.Utilities;
using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using Data_Layer.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

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
