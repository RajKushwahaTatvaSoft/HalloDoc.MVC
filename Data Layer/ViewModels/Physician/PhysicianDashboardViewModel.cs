using Data_Layer.CustomModels;
using Data_Layer.CustomModels.Filter;
using Data_Layer.CustomModels.TableRow.Physician;
using Data_Layer.DataModels;

namespace Data_Layer.ViewModels.Physician
{
    public class PhysicianDashboardViewModel
    {
        public PagedList<PhyDashboardTRow> pagedList {  get; set; }
        public IEnumerable<Casetag> casetags { get; set; }
        public IEnumerable<Region> regions { get; set; }
        public IEnumerable<DataModels.Physician> physicians { get; set; }
        public DashboardFilter filterOptions { get; set; }
        public string UserName { get; set; }
        public int DashboardStatus {  get; set; }
        public int NewReqCount { get; set; }
        public int PendingReqCount { get; set; }
        public int ActiveReqCount { get; set; }
        public int ConcludeReqCount { get; set; }
        public int CurrentPage {  get; set; }
    }
}
