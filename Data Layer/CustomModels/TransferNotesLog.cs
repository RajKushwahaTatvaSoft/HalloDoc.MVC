using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels
{
    public class TransferNotesLog
    {
        public int Status {  get; set; }
        public bool IsTransToAdmin { get; set; }
        public string AdminName { get; set; }
        public int? PhysicianId { get; set; }
        public int? AdminId { get; set; }
        public string PhysicianName { get; set; }
        public string TransToPhysicianName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Notes { get; set; }

    }
}
