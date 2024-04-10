using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class PatientRecordsViewModel
    {
        public string? UserName {  get; set; }
        public IEnumerable<Role>? roles { get; set; }
        public IEnumerable<User>? users { get; set; }
        public IEnumerable<Request>? requests { get; set; }
        public IEnumerable<exploreViewModel>? explores { get; set; }
    }

    public class exploreViewModel
    {
        public int? id { get; set; }
        public string? Name { get; set; }
        public string? confirmationNumber { get; set; }
        public DateTime? createdAt { get; set; }
        public string? providerName { get; set; }
        public DateTime? concludedDate { get; set; }
        public string? status { get; set; }
        public bool? finalReport { get; set; }
        public int? count { get; set; }
    }
}
