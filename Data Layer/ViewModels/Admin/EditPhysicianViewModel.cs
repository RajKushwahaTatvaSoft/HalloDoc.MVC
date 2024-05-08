using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels.Admin
{
    public class EditPhysicianViewModel
    {
        public string? LoggedInUserName { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<City>? selectedCities {  get; set; }
        public IEnumerable<Role>? roles { get; set; }
        public IEnumerable<int>? physicianRegions { get; set; }
        public IEnumerable<int>? selectedRegions { get; set; }
        public int? PhysicianId { get; set; }
        public string? PhyUserName { get; set; }

        [Required(ErrorMessage = "Password Cannot be empty")]
        [RegularExpression("(?=^.{8,}$)((?=.*\\d)|(?=.*\\W+))(?![.\\n])(?=.*[A-Z])(?=.*[a-z]).*$", ErrorMessage = "Password must contain 1 capital, 1 small, 1 Special symbol and at least 8 characters")]
        public string Password { get; set; }
        public int? StatusId { get; set; }

        [Required]
        public int? RoleId { get; set; }

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string Email { get; set; }
        public string? AdminNotes { get; set; }
        public string? MailCountryCode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        public string MailPhone { get; set; }
        public string? MedicalLicenseNumber { get; set; }
        public string? NPINumber { get; set; }
        public string? SyncEmail { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }

        [Required(ErrorMessage = "State cannot be empty")]
        public int? RegionId { get; set; }

        [Required(ErrorMessage = "City cannot be empty")]
        public int? CityId {  get; set; }
        public string? Zip { get; set; }
        public string? CountryCode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        public string Phone { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessWebsite { get; set; }
        public bool IsICA { get; set; } = false;
        public bool IsBGCheck{  get; set; } = false;
        public bool IsHIPAA{  get; set; } = false;
        public bool IsNDA{  get; set; } = false;
        public bool IsLicenseDoc{  get; set; } = false;

        /* POST METHOD FILES */
        public IFormFile? PhotoFile { get; set; }
        public IFormFile? SignatureFile { get; set; }
        public IFormFile? ICAFile { get; set; }
        public IFormFile? BGCheckFile { get; set; }
        public IFormFile? HIPAAComplianceFile { get; set; }
        public IFormFile? NDAFile { get; set; }
        public IFormFile? LicenseDocFile { get; set; }
        
    }

}
