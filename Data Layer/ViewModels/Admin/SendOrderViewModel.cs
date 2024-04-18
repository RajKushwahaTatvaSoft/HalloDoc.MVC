using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class SendOrderViewModel
    {
        public bool IsAdmin = false;
        public IEnumerable<Healthprofessional>? vendorsList { get; set; }
        public IEnumerable<Healthprofessionaltype>? professionalTypeList { get; set; }
        [Required(ErrorMessage = "Please select vendor")]
        public int SelectedVendor { get; set; }
        public int RequestId { get; set; }

        [Required(ErrorMessage = "Please enter business contact")]
        public string? BusinessContact { get; set; }

        [Required(ErrorMessage = "Please enter email")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Please enter fax number")]
        public string? FaxNumber { get; set; }
        public string? Prescription { get; set; }

        [Required(ErrorMessage = "Please select no of refills")]
        public int? NoOfRefills { get; set; }
    }
}
