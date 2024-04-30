using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels.Admin
{
    public class EncounterFormViewModel
    {
        public bool IsAdmin = false;

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? FirstName { get; set; }

        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? LastName { get; set; }
        public string? Location { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "DOB cannot be empty")]
        [DateNotInFuture(ErrorMessage = "Date Of Birth should be in past.")]
        public DateTime? DOB { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CountryCode{ get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^[0-9\\+\\- ]+$", ErrorMessage = "Enter valid Phone")]
        public string? PhoneNumber { get; set; }
        public string? History { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Medications { get; set; }
        public string? Allergies { get; set; }
        public string? Temp { get; set; }
        public string? HR { get; set; }
        public string? RR { get; set; }
        public string? BpLow { get; set; }
        public string? BpHigh { get; set; }
        public string? O2 { get; set; }
        public string? Pain { get; set; }
        public string? Heent { get; set; }
        public string? CV { get; set; }
        public string? Chest { get; set; }
        public string? ABD { get; set; }
        public string? Extr { get; set; }
        public string? Skin { get; set; }
        public string? Neuro { get; set; }
        public string? Other { get; set; }
        public string? Diagnosis { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? MedicationDispensed { get; set; }
        public string? Procedures { get; set; }
        public string? FollowUps { get; set; }
        public int RequestId { get; set; }

    }
}
