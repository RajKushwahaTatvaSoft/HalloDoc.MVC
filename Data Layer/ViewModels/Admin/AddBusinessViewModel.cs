using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class EditBusinessViewModel
    {
        public int? VendorId { get; set; }
        public string? BusinessName { get; set; }
        public int? ProfessionId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? BusinessContact { get; set; }
        public string? Street { get; set; }
        public string? Zip { get; set; }
        public int? RegionId { get; set; }
        public int? CityId { get; set; }
        public IEnumerable<Healthprofessionaltype>? professions { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<City>? selectedCities { get; set; }

    }
}
