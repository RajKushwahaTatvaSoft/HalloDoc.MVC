using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class AdminProfileViewModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? ConfirmEmail { get; set; }
        public string? Password { get; set; }
        public string? CountryCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public short? Status { get; set; }
        public int? Role { get; set; }
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<int>? selectedRegions { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? State { get; set; }
        public int RegionId {  get; set; }
        public string? AltCountryCode { get; set; }
        public string? AltPhoneNumber { get; set; }

    }

}
