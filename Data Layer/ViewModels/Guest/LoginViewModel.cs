using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels.Guest
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "User Name cannot be empty")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Password cannot be empty")]
        public string? Password { get; set; }
    }
}
