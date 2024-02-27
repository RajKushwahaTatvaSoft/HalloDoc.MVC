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
        public string? Email { get; set; }
        public string? Countrycode { get; set; }
        [Required(ErrorMessage = "Phone cannot be empty")]
        public string? Phone { get; set; }
        [Required(ErrorMessage = "Street cannot be empty")]
        public string? Street { get; set; }
        [Required(ErrorMessage = "City cannot be empty")]
        public string? City { get; set; }
        [Required(ErrorMessage = "State cannot be empty")]
        public string? State { get; set; }
        [Required(ErrorMessage = "Zip Code cannot be empty")]
        public string? ZipCode { get; set; }
        public string? RoomSuite { get; set; }
        [Required(ErrorMessage = "Password cannot be empty")]
        public string? Password { get; set; }
        [Compare("Password", ErrorMessage = "Password and Confirm Password should be same.")]
        public string? ConfirmPassword { get; set; }
        public IFormFile? File {  get; set; }
    }
}
