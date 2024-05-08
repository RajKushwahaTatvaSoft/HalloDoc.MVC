using Data_Layer.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Admin
{
    public class CreateAccessViewModel
    {
        [Required]
        public string RoleName { get; set; } = string.Empty;
        [Required]
        public int? AccountType { get; set; }
        public IEnumerable<Aspnetrole>? netRoles { get; set; }
        [Required(ErrorMessage = "Please select menu")]
        public IEnumerable<int>? selectedMenus {  get; set; }
    }
}
