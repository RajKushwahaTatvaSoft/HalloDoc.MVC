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
        public string? Relation { get; set; }
    }
}