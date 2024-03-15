using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class CloseCaseViewModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? Dob { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public List<Requestwisefile> Files { get; set; }
        public int requestid { get; set; }
        public string? confirmatioNumber { get; set; }

    }
}
