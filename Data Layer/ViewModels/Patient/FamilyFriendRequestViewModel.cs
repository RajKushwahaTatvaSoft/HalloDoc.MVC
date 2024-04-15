using Data_Layer.DataModels;
using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels
{
    public class FamilyFriendRequestViewModel
    {
        public PatientRequestViewModel patientDetails { get; set; }
        [Required(ErrorMessage = "First Name cannot be empty")]
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Countrycode { get; set; }
        [Required(ErrorMessage = "Phone cannot be empty")]
        public string? Phone { get; set; }
        [Required(ErrorMessage = "Email cannot be empty")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Relation cannot be empty")]
        public string? Relation { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public bool? IsValidated { get; set; }
    }
}