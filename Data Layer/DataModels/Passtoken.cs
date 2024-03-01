using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data_Layer.DataModels;

[Table("passtoken")]
[Index("Uniquetoken", Name = "passtoken_uniquetoken_key", IsUnique = true)]
public partial class Passtoken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("email")]
    [StringLength(50)]
    public string Email { get; set; } = null!;

    [Column("isdeleted")]
    public bool Isdeleted { get; set; }

    [Column("createddate", TypeName = "timestamp without time zone")]
    public DateTime Createddate { get; set; }

    [Column("aspnetuserid")]
    public string Aspnetuserid { get; set; } = null!;

    [Column("isresettoken")]
    public bool Isresettoken { get; set; }

    [Column("uniquetoken")]
    public string Uniquetoken { get; set; } = null!;
}
