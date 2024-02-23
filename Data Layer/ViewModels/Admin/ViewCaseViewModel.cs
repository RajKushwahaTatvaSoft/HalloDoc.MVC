using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class ViewCaseViewModel
    {
        public string? patientNotes { get; set; }

        public string? patientName { get; set; }

        public string? patientFirstName { get; set; }

        public string? patientLastName { get; set; }

        public string? patientEmail { get; set; }
        public string? mobileType { get; set; }

        public string? patientPhone { get; set; }
        public int? countryCode { get; set; }
        public DateTime? dob { get; set; }
        public int? region { get; set; }
        public string? rooms { get; set; }
        public string? address { get; set; }

        public string? zip { get; set; }
        public string? confirmation { get; set; }

        public int requestId { get; set; }
        public string? notes { get; set; }
        public List<Requestclient> requestclient { get; set; }

    }
}
