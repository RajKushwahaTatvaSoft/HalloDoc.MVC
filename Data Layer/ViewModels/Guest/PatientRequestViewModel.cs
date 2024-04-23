using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels.Guest
{
    public class PatientRequestViewModel
    {
        public string? Symptom { get; set; }

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string FirstName { get; set; } = string.Empty;

        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "DOB cannot be empty")]
        [DateNotInFuture(ErrorMessage = "Date Of Birth should be in past.")]
        public DateTime? DOB { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string Email { get; set; }

        public string? Countrycode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^[0-9\\+\\-]+$", ErrorMessage = "Enter valid Phone")]
        public string? Phone { get; set; }
        public string? Street { get; set; }

        [Required(ErrorMessage = "State cannot be empty")]
        public int? RegionId { get; set; }

        [Required(ErrorMessage = "City cannot be empty")]
        public int? CityId { get; set; }
        public string? ZipCode { get; set; }
        public string? RoomSuite { get; set; }

        [RegularExpression("(?=^.{8,}$)((?=.*\\d)|(?=.*\\W+))(?![.\\n])(?=.*[A-Z])(?=.*[a-z]).*$", ErrorMessage = "Password must contain 1 capital, 1 small, 1 Special symbol and at least 8 characters")]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Password and Confirm Password should be same.")]
        public string? ConfirmPassword { get; set; }

        public IFormFile? File { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<City>? selectedRegionCities { get; set; }
        public bool? IsValidated { get; set; }
    }

}