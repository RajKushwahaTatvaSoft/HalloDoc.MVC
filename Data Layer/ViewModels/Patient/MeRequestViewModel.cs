using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class MeRequestViewModel
    {
        public int? UserId { get; set; }
        public string? Symptom { get; set; }

        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? FirstName { get; set; }

        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "DOB cannot be empty")]
        [DateNotInFuture(ErrorMessage = "Date Of Birth should be in past.")]
        public DateTime? DOB { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }
        public string? Countrycode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^[0-9\\+\\-]+$", ErrorMessage = "Enter valid Phone")]
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? RoomSuite { get; set; }

        [Required(ErrorMessage = "State cannot be empty")]
        public int? RegionId { get; set; }

        [Required(ErrorMessage = "City cannot be empty")]
        public int? CityId { get; set; }

        public IFormFile? File { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<City>? selectedRegionCities { get; set; }
    }
}
