using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.CustomModels.TableRow.Admin
{
    public class ProviderPayrateTRow
    {
        public int? PayrateId { get; set; }
        public int ProviderId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal? PayrateAmount { get; set; }
    }
}
