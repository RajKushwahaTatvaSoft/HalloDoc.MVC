using Data_Layer.DataModels;
using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminProfileViewModel
    {
        public string? UserName {  get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<int>? selectedRegions { get; set; }
        public IEnumerable<City>? adminMailCities { get; set; }
        public IEnumerable<Role>? roles {  get; set; }
        public int? AdminId { get; set; }

        public ProfileAdministratorInfo? AdministratorInfo { get; set; }
        
        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Confirm Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        [Compare(nameof(Email),ErrorMessage ="Email and confirm email should be same.")]
        public string? ConfirmEmail { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression("(?=^.{8,}$)((?=.*\\d)|(?=.*\\W+))(?![.\\n])(?=.*[A-Z])(?=.*[a-z]).*$", ErrorMessage = "Password must contain 1 capital, 1 small, 1 Special symbol and at least 8 characters")]
        public string? Password { get; set; }
        public string? CountryCode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^[0-9\\+\\-]+$", ErrorMessage = "Enter valid Phone")]
        public string? PhoneNumber { get; set; }
        public string? AspUserName { get; set; }
        public int? StatusId { get; set; }

        [Required(ErrorMessage = "Role cannot be empty")]
        public int? RoleId { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? State { get; set; }

        [Required(ErrorMessage = "State cannot be empty")]
        public int RegionId {  get; set; }

        [Required(ErrorMessage = "City cannot be empty")]
        public int CityId {  get; set; }

        public string? AltCountryCode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^[0-9\\+\\-]+$", ErrorMessage = "Enter valid Phone")]
        public string? AltPhoneNumber { get; set; }

    }

    public class ProfileAdministratorInfo
    {
        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-s]{}[\\.],1}[A-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Confirm Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        [Compare(nameof(Email), ErrorMessage ="Email and Confirm Email should be same")]
        public string? ConfirmEmail { get; set; }

        public string? PhoneNumber { get; set; }

        public IEnumerable<int>? selectedRegions { get; set; }

    }
}
