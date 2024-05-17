using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data_Layer.DataModels;

[Table("UserConnection")]
public partial class UserConnection
{
    [Key]
    public int Id { get; set; }

    [StringLength(128)]
    public string UserAspNetUserId { get; set; } = null!;

    [StringLength(128)]
    public string SignalConnectionId { get; set; } = null!;
}
