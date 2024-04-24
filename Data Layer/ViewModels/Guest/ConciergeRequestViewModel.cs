﻿using Data_Layer.DataModels;
using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels.Guest
{
    public class ConciergeRequestViewModel
    {
        public PatientRequestViewModel patientDetails { get; set; }

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string FirstName { get; set; }

        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? LastName { get; set; }
        public string Countrycode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^[0-9\\+\\-]+$", ErrorMessage = "Enter valid Phone")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hotel name cannot be empty")]
        public string? HotelOrPropertyName { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public bool? IsValidated { get; set; }
    }
}