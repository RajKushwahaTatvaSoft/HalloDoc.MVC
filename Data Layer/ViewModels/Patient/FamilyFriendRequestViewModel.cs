using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class FamilyFriendRequestViewModel
    {
        public PatientRequestViewModel patientDetails { get; set; }
        [Required(ErrorMessage = "First Name cannot be empty")]
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Countrycode { get; set; }
        [Required(ErrorMessage = "Phonecannot be empty")]
        public string? Phone { get; set; }
        [Required(ErrorMessage = "Email cannot be empty")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Relation cannot be empty")]
        public string? Relation { get; set; }
        public IEnumerable<Region>? regions { get; set; }
    }
}