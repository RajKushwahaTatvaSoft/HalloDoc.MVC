using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class EditAccessViewModel
    {
        [Required]
        public int RoleId { get; set; }
        [Required]
        public string? RoleName { get; set; }
        [Required]
        public int? AccountType { get; set; } = 0;
        public IEnumerable<Aspnetrole>? netRoles { get; set; }
        public IEnumerable<int> selectedMenus { get; set; }
        public IEnumerable<Menu>? Menus { get; set; }
    }
}
