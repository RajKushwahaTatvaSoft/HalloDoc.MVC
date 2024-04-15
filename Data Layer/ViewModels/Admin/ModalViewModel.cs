
using Data_Layer.DataModels;
using System.ComponentModel.DataAnnotations;

namespace Data_Layer.ViewModels.Admin
{

    public class AssignCaseModel
    {
        public int? RequestId { get; set; }
        public int? PhysicianId { get; set; }
        public IEnumerable<Region> regions { get; set; }
        public string Notes { get; set; }
    }

    public class DayShiftModel
    {
        public DateTime ShiftDate { get; set; }
        public IEnumerable<Shiftdetail> shiftdetails { get; set; }
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
        [Required(ErrorMessage = "First Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name cannot be empty")]
        [RegularExpression("^[A-Za-z\\s]{1,}[\\.]{0,1}[A-Za-z\\s]{0,}$", ErrorMessage = "Enter Valid Name")]
        public string LastName { get; set; }
        public string CountryCode { get; set; }

        [Required(ErrorMessage = "Phone cannot be empty")]
        [RegularExpression("^\\d+$", ErrorMessage = "Only numbers are allowed")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email cannot be empty")]
        [RegularExpression("^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$", ErrorMessage = "Enter Valid Email")]
        public string Email { get; set; }
    }

    public class ContactYourProviderModel
    {
        public int PhysicianId { get; set; }
        public int CommunicationType { get; set; }
        public string Message { get; set; }
    }

    public class ViewShiftModel
    {
        public int ShiftDetailId { get; set; }
        public IEnumerable<Region> regions { get; set; }
        public int RegionId { get; set; }
        public IEnumerable<DataModels.Physician> selectedPhysicians { get; set; }
        public int PhysicianId { get; set; }
        public DateTime ShiftDate { get; set; }
        public TimeOnly ShiftStartTime { get; set; }
        public TimeOnly ShiftEndTime { get; set; }
    }


}
