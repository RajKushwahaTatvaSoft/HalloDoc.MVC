using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class ViewUploadsViewModel
    {
        public string UserName { get; set; }
        public string PatientName { get; set; }
        public string ConfirmationNumber { get; set;}
        public int RequestId { get; set;}
        public IFormFile? File { get; set; }
        public List<Requestwisefile> requestwisefiles { get; set; }
        public List<string> extensions { get; set; }
    }
}
