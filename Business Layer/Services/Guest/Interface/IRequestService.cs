using Data_Layer.ViewModels;
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
        public Dictionary<string,object> SubmitPatientRequest(PatientRequestViewModel model);
    }
}
