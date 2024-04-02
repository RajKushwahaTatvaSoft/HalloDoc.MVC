using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class EditPhysicianViewModel
    {
        public IEnumerable<Region>? regions { get; set; }
        public IEnumerable<Role>? roles { get; set; }
        public IEnumerable<int>? physicianRegions { get; set; }
        public IEnumerable<int>? selectedRegions { get; set; }
        public int? PhysicianId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int StatusId { get; set; }
        public int RoleId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string AdminNotes { get; set; }
        public string MailCountryCode { get; set; }
        public string MailPhone { get; set; }
        public string MedicalLicenseNumber { get; set; }
        public string NPINumber { get; set; }
        public string? SyncEmail { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public int? RegionId { get; set; }
        public string Zip { get; set; }
        public string CountryCode { get; set; }
        public string Phone { get; set; }
        public string BusinessName { get; set; }
        public string BusinessWebsite { get; set; }
        public bool IsICA {  get; set; }
        public bool IsBGCheck{  get; set; }
        public bool IsHIPAA{  get; set; }
        public bool IsNDA{  get; set; }
        public bool IsLicenseDoc{  get; set; }

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
