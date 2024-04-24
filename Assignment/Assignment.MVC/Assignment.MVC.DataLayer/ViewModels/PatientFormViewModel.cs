using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assignment.MVC.DataLayer.ViewModels
{
    public class PatientFormViewModel
    {
        public bool IsEdit { get; set; } = false;
        public int PatientId { get; set; }

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? PatientFirstName { get; set;}

        [Required(ErrorMessage = "Last Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? PatientLastName { get; set;}

        public int DoctorId {  get; set; }

        [Required(ErrorMessage = "Age cannot be empty")]
        public int? Age { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email {  get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^[0-9\\+\\- ]+$", ErrorMessage = "Enter valid Phone")]
        public string? PhoneNumber { get; set; }

        public string? CountryCode {  get; set; }

        [Required(ErrorMessage = "Gender cannot be empty")]
        public string? Gender {  get; set; }

        [Required(ErrorMessage = "Disease cannot be empty")]
        public string? Disease { get; set; }

        [Required(ErrorMessage = "Specialist cannot be empty")]
        public string Specialist { get; set; }

        public IEnumerable<string>? DiseaseList { get; set; }
        public IEnumerable<string>? DoctorList {  get; set; } 
    }
}