using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class ForgotPasswordViewModel
    {
        public string? Email { get; set;}
        public string? Password { get; set;}
        [Compare("Password", ErrorMessage = "Password and Confirm Password should be same.")]
        public string? ConfirmPassword { get; set; }
    }
}
