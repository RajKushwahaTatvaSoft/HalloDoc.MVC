using Data_Layer.ViewModels.Admin;

namespace Data_Layer.ViewModels.Physician
{
    public class TimeSheetFormViewModel
    {
        public int TimesheetId { get; set; }
        public bool IsReceiptsAdded {  get; set; } = false;
        public List<TimeSheetDayRecord>? timeSheetDayRecords { get; set; }
        public List<ReceiptRecord>? timeSheetReceiptRecords {  get; set; }
        public ReceiptPartialViewModel? ReceiptPartialViewModel { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}