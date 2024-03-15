using Data_Layer.DataModels;

namespace Data_Layer.ViewModels
{
    public class ConciergeRequestViewModel
    {
        public PatientRequestViewModel patientDetails {  get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Countrycode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string? HotelOrPropertyName { get; set; }
        public IEnumerable<Region>? regions { get; set; }
    }
}
