namespace Data_Layer.CustomModels.Filter
{
    public class DashboardFilter
    {
        public string PatientSearchText { get; set; }
        public int RegionFilter { get; set; }
        public int RequestTypeFilter { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Status { get; set; }
    }
}