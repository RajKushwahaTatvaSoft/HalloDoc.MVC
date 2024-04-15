using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels
{
    public enum ResponseCode
    {
        Success = 200,
        Error = 500,
    }
    public class ServiceResponse
    {
        public ResponseCode StatusCode {  get; set; }
        public string? Message { get; set; }
    }
}
