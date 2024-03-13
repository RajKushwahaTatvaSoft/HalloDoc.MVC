using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class ViewDocumentViewModel
    {
        public string UserName { get; set; }
        public int RequestId { get; set; }
        public string ConfirmationNumber { get; set; }
        public List<Requestwisefile> requestwisefiles { get; set; }
        public IFormFile File { get; set; }
    }
}
