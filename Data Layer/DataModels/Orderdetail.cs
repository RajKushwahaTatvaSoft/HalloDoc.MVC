using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Orderdetail
{
    public int Id { get; set; }

    public int? Vendorid { get; set; }

    public int? Requestid { get; set; }

    public string? Faxnumber { get; set; }

    public string? Email { get; set; }

    public string? Businesscontact { get; set; }

    public string? Prescription { get; set; }

    public int? Noofrefill { get; set; }

    public DateTime? Createddate { get; set; }

    public string? Createdby { get; set; }
}
