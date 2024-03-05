using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class ViewCaseViewModel
    {
        public string? UserName { get; set; }
        public string? PatientNotes { get; set; }
        public string? PatientName { get; set; }
        public string? PatientFirstName { get; set; }
        public string? PatientLastName { get; set; }
        public string? PatientEmail { get; set; }
        public string? MobileType { get; set; }
        public string? PatientPhone { get; set; }
        public int? CountryCode { get; set; }
        public DateTime? Dob { get; set; }
        public int? Region { get; set; }
        public string? Rooms { get; set; }
        public string? Address { get; set; }
        public string? Zip { get; set; }
        public string? Confirmation { get; set; }
        public int RequestType {  get; set; }
        public int DashboardStatus {  get; set; }
        public int RequestId { get; set; }
        public string? Notes { get; set; }
        public IEnumerable<Casetag> casetags { get; set; }
        public IEnumerable<Region> regions { get; set; }
        public IEnumerable<Physician> physicians { get; set; }

    }
}
