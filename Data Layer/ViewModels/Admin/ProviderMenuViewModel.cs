using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class ProviderMenuViewModel
    {
        public string UserName { get; set; }
        public IEnumerable<DataModels.Physician> physicians { get; set; }
    }
}
