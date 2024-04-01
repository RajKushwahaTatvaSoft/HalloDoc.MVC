
using Data_Layer.CustomModels;

namespace Data_Layer.ViewModels
{
    public class ProviderLocationViewModel
    {
        public IEnumerable<PhyLocationRow> locationList { get; set; }
        public string ApiKey { get; set; }
    }
}
