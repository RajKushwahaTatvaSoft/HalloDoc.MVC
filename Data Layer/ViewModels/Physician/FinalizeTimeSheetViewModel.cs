namespace Data_Layer.ViewModels.Physician
{
    public class FinalizeTimeSheetViewModel
    {
        public IEnumerable<TimeSheetDayRecord>? timeSheetDayRecords { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}