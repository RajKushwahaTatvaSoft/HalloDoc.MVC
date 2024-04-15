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

        [Required(ErrorMessage = "Password Cannot be empty")]
        [RegularExpression("(?=^.{8,}$)((?=.*\\d)|(?=.*\\W+))(?![.\\n])(?=.*[A-Z])(?=.*[a-z]).*$", ErrorMessage = "Password must contain 1 capital, 1 small, 1 Special symbol and at least 8 characters")]
        public string? Password { get; set;}

        [Required(ErrorMessage = "Confirm password Cannot be empty")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password should be same.")]
        public string? ConfirmPassword { get; set; }
    }
}
