using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class CreateAccessViewModel
    {
        public int RoleId { get; set; }
        public string? menuName { get; set; }
        public string? menuId { get; set; }
        public string? roleName { get; set; }
        public int? accounttype { get; set; }
        public IEnumerable<Aspnetrole> netRoles { get; set; }
        public IEnumerable<Menu>? Menus { get; set; }
        public IEnumerable<int>? selectedRoles {  get; set; } 
    }
}
