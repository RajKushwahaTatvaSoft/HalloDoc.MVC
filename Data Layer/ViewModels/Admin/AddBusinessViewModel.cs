using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class EditBusinessViewModel
    {
        public int? VendorId { get; set; }

        [Required(ErrorMessage = "Business Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? BusinessName { get; set; }
        public int? ProfessionId { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^\\d+$", ErrorMessage = "Only numbers are allowed")]
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? BusinessContact { get; set; }
        public string? Street { get; set; }
        public string? Zip { get; set; }

        [Required(ErrorMessage = "State cannot be empty")]
        public int? RegionId { get; set; }

        [Required(ErrorMessage = "City cannot be empty")]
        public int? CityId { get; set; }
        public IEnumerable<Healthprofessionaltype>? professions { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<City>? selectedCities { get; set; }

    }
}
