using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Admin
{
    public class LogTableRow
    {
        public string RecipientName { get; set; }  
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public string Action { get; set; }
        public string RoleName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SentDate { get; set;}
        public int SentTries { get; set; }
        public bool IsSent { get; set; }
        public string ConfirmationNumber { get; set; }
    }
}