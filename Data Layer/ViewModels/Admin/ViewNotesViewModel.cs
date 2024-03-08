using Data_Layer.CustomModels;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class ViewNotesViewModel
    {
        public string UserName { get; set; }
        public int RequestId { get; set; }
        public string AdminNotes { get; set; }
        public string PhysicianNotes { get; set; }
        public IEnumerable<TransferNotesLog> transferNotesLogs { get; set; }
    }
}
