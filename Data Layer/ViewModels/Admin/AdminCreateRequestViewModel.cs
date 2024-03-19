using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminCreateRequestViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? countryCode { get; set; }
        public string? phoneNumber { get; set; }
        public string? email { get; set; }
        public DateTime? dob { get; set; }
        public string? street { get; set; }
        public string? city { get; set; }
        public int? state { get; set; }
        public string? stateName { get; set; }
        public string? zipCode { get; set; }
        public string? room { get; set; }
        public string? notes { get; set; }
        public IEnumerable<Region>? regions { get; set; }

    }


}
