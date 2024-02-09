using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Healthprofessional
{
    public int Vendorid { get; set; }

    public string Vendorname { get; set; } = null!;

    public int? Profession { get; set; }

    public string Faxnumber { get; set; } = null!;

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Zip { get; set; }

    public int? Regionid { get; set; }

    public DateTime Createddate { get; set; }

    public DateTime? Modifieddate { get; set; }

    public string? Phonenumber { get; set; }

    public bool? Isdeleted { get; set; }

    public string? Ip { get; set; }

    public string? Email { get; set; }

    public string? Businesscontact { get; set; }

    public virtual Healthprofessionaltype? ProfessionNavigation { get; set; }
}
