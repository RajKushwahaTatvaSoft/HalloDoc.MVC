using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Requestwisefile
{
    public int Requestwisefileid { get; set; }

    public int Requestid { get; set; }

    public string Filename { get; set; } = null!;

    public DateTime Createddate { get; set; }

    public int? Physicianid { get; set; }

    public int? Adminid { get; set; }

    public short? Doctype { get; set; }

    public bool? Isfrontside { get; set; }

    public bool? Iscompensation { get; set; }

    public string? Ip { get; set; }

    public bool? Isfinalize { get; set; }

    public bool? Isdeleted { get; set; }

    public bool? Ispatinetrecords { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual Physician? Physician { get; set; }

    public virtual Request Request { get; set; } = null!;
}
