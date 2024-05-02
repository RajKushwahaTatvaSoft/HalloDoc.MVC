using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data_Layer.DataModels;

[Table("ProviderPayrate")]
public partial class ProviderPayrate
{
    [Key]
    public int PayrateId { get; set; }

    public int PhysicianId { get; set; }

    public int? Shift { get; set; }

    public int? PhoneConsult { get; set; }

    public int? HouseCall { get; set; }

    public int? BatchTesting { get; set; }

    public int? NightShiftWeekend { get; set; }

    public int? HouseCallNightWeekend { get; set; }

    public int? PhoneConsultNightWeekend { get; set; }

    [StringLength(128)]
    public string CreatedBy { get; set; } = null!;

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? CreatedDate { get; set; }

    [StringLength(128)]
    public string? ModifiedBy { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? ModifiedDate { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("ProviderPayrateCreatedByNavigations")]
    public virtual Aspnetuser CreatedByNavigation { get; set; } = null!;

    [ForeignKey("ModifiedBy")]
    [InverseProperty("ProviderPayrateModifiedByNavigations")]
    public virtual Aspnetuser? ModifiedByNavigation { get; set; }

    [ForeignKey("PhysicianId")]
    [InverseProperty("ProviderPayrates")]
    public virtual Physician Physician { get; set; } = null!;
}
