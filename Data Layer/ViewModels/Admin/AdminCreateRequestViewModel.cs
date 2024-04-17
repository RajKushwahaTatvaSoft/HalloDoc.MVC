using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminCreateRequestViewModel
    {
        public string? UserName { get; set; }
        public bool IsAdmin { get; set; } = false;

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string FirstName { get; set; }

        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string LastName { get; set; }
        public string? CountryCode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^\\d+$",ErrorMessage = "Only numbers are allowed")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "DOB cannot be empty")]
        [DateNotInFuture(ErrorMessage = "Date Of Birth should be in past.")]
        public DateTime? DOB { get; set; }
        public string? Street { get; set; }

        [Required(ErrorMessage = "State cannot be empty")]
        public int RegionId { get; set; }

        [Required(ErrorMessage = "City cannot be empty")]
        public int CityId {  get; set; }
        public string? ZipCode { get; set; }
        public string? Room { get; set; }
        public string? Notes { get; set; }
        public IEnumerable<Region>? regions { get; set; }

    }


}
