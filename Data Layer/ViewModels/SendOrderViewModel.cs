using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class SendOrderViewModel
    {
        public string UserName { get; set; }
        public IEnumerable<Healthprofessional>? vendorsList {  get; set; }
        public IEnumerable<Healthprofessionaltype>? professionalTypeList {  get; set; }
        public int SelectedVendor { get; set; }
        public int RequestId { get; set; }
        public string BusinessContact { get; set; }
        public string Email { get; set; }
        public string FaxNumber { get; set; }
        public string Prescription { get; set; }
        public int NoOfRefills { get; set; }
    }
}
