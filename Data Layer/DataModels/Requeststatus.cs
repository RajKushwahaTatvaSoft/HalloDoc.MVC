using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data_Layer.DataModels;

[Table("requeststatus")]
public partial class Requeststatus
{
    [Key]
    [Column("statusid")]
    public int Statusid { get; set; }

    [Column("statusname")]
    [StringLength(20)]
    public string Statusname { get; set; } = null!;
}
