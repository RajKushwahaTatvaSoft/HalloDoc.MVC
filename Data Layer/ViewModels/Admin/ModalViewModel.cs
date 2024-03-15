
using Data_Layer.DataModels;

namespace Data_Layer.ViewModels.Admin
{
    public class ModalViewModel
    {
        public class AssignCaseModel
        {
            public int RequestId { get; set; }
            public int PhysicianId { get; set; }
            public IEnumerable<Region> regions { get; set; }
            public string Notes {  get; set; }
        }
    }
}
