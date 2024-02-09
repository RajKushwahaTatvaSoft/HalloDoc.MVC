using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Smslog
{
    public decimal Smslogid { get; set; }

    public string Smstemplate { get; set; } = null!;

    public string Mobilenumber { get; set; } = null!;

    public string? Confirmationnumber { get; set; }

    public int? Roleid { get; set; }

    public int? Adminid { get; set; }

    public int? Requestid { get; set; }

    public int? Physicianid { get; set; }

    public DateTime Createdate { get; set; }

    public DateTime? Sentdate { get; set; }

    public bool? Issmssent { get; set; }

    public int Senttries { get; set; }

    public int? Action { get; set; }
}
