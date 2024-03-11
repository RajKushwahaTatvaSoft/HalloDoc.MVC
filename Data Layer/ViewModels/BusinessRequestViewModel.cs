using Data_Layer.DataModels;

namespace Data_Layer.ViewModels
{
    public class BusinessRequestViewModel
    {
        public PatientRequestViewModel patientDetails { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Countrycode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string BusinessOrPropertyName { get; set; }
        public string CaseNumber { get; set; }
        public IEnumerable<Region> regions {  get; set; }
    }
}
