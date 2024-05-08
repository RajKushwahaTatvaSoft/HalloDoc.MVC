using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data_Layer.DataModels;

[Table("PayrateCategory")]
public partial class PayrateCategory
{
    [Key]
    public int PayrateCategoryId { get; set; }

    [StringLength(256)]
    public string CategoryName { get; set; } = null!;

    [InverseProperty("PayrateCategory")]
    public virtual ICollection<ProviderPayrate> ProviderPayrates { get; set; } = new List<ProviderPayrate>();
}
