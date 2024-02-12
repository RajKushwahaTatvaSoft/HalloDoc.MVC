using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class FamilyFriendRequestViewModel
    {
        public PatientRequestViewModel patientDetails { get; set; } 
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Relation { get; set; }

    }
}
