using Data_Layer.CustomModels.TableRow.Admin;
using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class UserAccessViewModel
    {
        public IEnumerable<Region> regions {  get; set; }
    }
}
