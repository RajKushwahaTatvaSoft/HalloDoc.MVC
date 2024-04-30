using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class PatientProfileViewModel
    {

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? FirstName { get; set; }

        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "DOB cannot be empty")]
        [DateNotInFuture(ErrorMessage = "Date Of Birth should be in past.")]
        public DateTime? DateOfBirth { get; set; }
        public string? Type { get; set; }
        public string? CountryCode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }
        public string? Street { get; set; }
        
        public string? City { get; set; }
        public int? RegionId {  get; set; }
        public int? CityId {  get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Location { get; set; }
        public IEnumerable<City>? selectedCities {  get; set; }
        public IEnumerable<Region>? regions { get; set; }
    }
}
