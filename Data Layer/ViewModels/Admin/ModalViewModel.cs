
using Data_Layer.DataModels;

namespace Data_Layer.ViewModels.Admin
{

    public class AssignCaseModel
    {
        public int? RequestId { get; set; }
        public int? PhysicianId { get; set; }
        public IEnumerable<Region> regions { get; set; }
        public string Notes { get; set; }
    }

    public class CancelCaseModel
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; }
        public int ReasonId { get; set; }
        public IEnumerable<Casetag> casetags { get; set; }
        public string Notes { get; set; }
    }

    public class BlockCaseModel
    {
        public int RequestId { get; set; }
        public string PatientName { get; set; }
        public string Reason { get; set; }

    }

    public class ClearCaseModel
    {
        public int RequestId { get; set; }
    }

    public class SendAgreementModel
    {
        public int RequestId { get; set; }
        public int RequestType { get; set; }
        public string PatientPhone { get; set; }
        public string PatientEmail { get; set; }

    }

    public class SendLinkModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CountryCode { get; set; }
        public string Phone {  get; set; }
        public string Email { get; set; }
    }

    public class ContactYourProviderModel
    {
        public int PhysicianId { get; set;}
        public int CommunicationType {  get; set; }
        public string Message { get; set; }
    }

    public class ViewShiftModel
    {
        public int ShiftDetailId {  get; set; }
        public IEnumerable<Region> regions { get; set; }
        public int RegionId {  get; set; }
        public IEnumerable<DataModels.Physician> selectedPhysicians { get; set; }
        public int PhysicianId { get; set;}
        public DateTime ShiftDate {  get; set; }
        public TimeOnly ShiftStartTime {  get; set; }
        public TimeOnly ShiftEndTime {  get; set; }
    }


}
