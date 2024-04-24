using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Assignment.MVC.DataLayer.DataModels;

[Table("Doctor")]
public partial class Doctor
{
    [Key]
    public int DoctorId { get; set; }

    [StringLength(50)]
    public string Specialist { get; set; } = null!;

    [InverseProperty("Doctor")]
    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
