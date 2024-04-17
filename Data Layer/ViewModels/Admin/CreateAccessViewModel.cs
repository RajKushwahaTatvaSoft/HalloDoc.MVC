﻿using Data_Layer.DataModels;
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
        public int RoleId { get; set; }
        public string? MenuName { get; set; }
        public string? MenuId { get; set; }
        public string? RoleName { get; set; }
        public int? AccountType { get; set; }
        public IEnumerable<Aspnetrole>? netRoles { get; set; }
        [Required(ErrorMessage = "Please select menu")]
        public IEnumerable<int>? selectedMenus {  get; set; }
    }
}
