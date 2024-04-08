using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Physician
{
    public class ConcludeCareViewModel
    {
        public string? UserName {  get; set; }
        public string? PatientName { get; set; }
        public string? ProviderNotes { get; set;}
        public IEnumerable<string>? fileNames { get; set;}
        public int? RequestId {  get; set; }
    }



}
