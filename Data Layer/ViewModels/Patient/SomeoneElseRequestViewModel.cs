using Data_Layer.DataModels;
using Data_Layer.ViewModels.Guest;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class SomeoneElseRequestViewModel
    {
        public string? Username { get; set; }
        public PatientRequestViewModel patientDetails { get; set; }

        [Required(ErrorMessage = "Relation cannot be empty")]
        public string? Relation { get; set; }
        public IEnumerable<Region>? regions { get; set; }
    }
}