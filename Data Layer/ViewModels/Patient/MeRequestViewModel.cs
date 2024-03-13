using Data_Layer.DataModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_Layer.ViewModels
{
    public class MeRequestViewModel
    {
        public int? UserId { get; set; }
        public string? Symptom { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DOB { get; set; }
        public string? Email { get; set; }
        public string? Countrycode { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? RoomSuite { get; set; }
        public int? RegionId { get; set; }
        public string? Password { get; set; }
        [Compare(nameof(Password), ErrorMessage = "Password and Confirm Password should be same.")]
        public string? ConfirmPassword { get; set; }
        public IFormFile? File { get; set; }
        public IEnumerable<Region>? regions { get; set; }
    }
}
