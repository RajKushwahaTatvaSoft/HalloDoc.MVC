using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Admin
{
    public class SearchRecordTRow
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; }
        public string Requestor { get; set;}
        public DateTime DateOfService {  get; set; }
        public DateTime CloseCaseDate {  get; set; }
        public string Email {  get; set; }
        public string PhoneNumber { get; set; }
        public string Address {  get; set; }
        public string Zip { get; set; }
        public string RequestStatus {  get; set; }
        public string Physician { get; set; }
        public string PhysicianNote { get; set; }
        public string CancelledByPhysicianNote { get; set; }
        public string AdminNote { get; set; }
        public string PatientNote { get; set; }
    }
}
