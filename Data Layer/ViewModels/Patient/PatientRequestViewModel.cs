using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels
{
    public class PatientRequestViewModel
    {
        public string? Symptom { get; set; }
        [Required(ErrorMessage = "First Name cannot be empty")]
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DOB { get; set; }
        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }
        public string? Countrycode { get; set; }
        [Required(ErrorMessage = "Phone cannot be empty")]
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public int? RegionId { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? RoomSuite { get; set; }
        [RegularExpression("(?=^.{8,}$)((?=.*\\d)|(?=.*\\W+))(?![.\\n])(?=.*[A-Z])(?=.*[a-z]).*$", ErrorMessage = "Password must contain 1 capital, 1 small, 1 Special symbol and at least 8 characters")]
        public string? Password { get; set; }
        [Compare("Password", ErrorMessage = "Password and Confirm Password should be same.")]
        public string? ConfirmPassword { get; set; }
        public IFormFile? File {  get; set; }
        public IEnumerable<Region>? regions { get; set; }
    }
}