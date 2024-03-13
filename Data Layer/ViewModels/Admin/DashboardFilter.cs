namespace Data_Layer.ViewModels.Admin
{
    public class DashboardFilter
    {
        public string PatientSearchText {  get; set; }
        public int RegionFilter {  get; set; }
        public int RequestTypeFilter { get; set; }
        public int pageNumber {  get; set; }
        public int pageSize { get; set; }
        public int status { get; set; }
    }
}