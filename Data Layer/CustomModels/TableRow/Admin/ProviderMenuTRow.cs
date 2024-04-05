using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Admin
{
    public class ProviderMenuTRow
    {
        public int PhysicianId { get; set; }
        public bool IsNotificationStopped { get; set; }
        public string PhysicianName { get; set; }
        public string Role { get; set; }
        public string OnCallStatus { get; set; }
        public string Status { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

    }
}
