using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Healthprofessionaltype
{
    public int Healthprofessionalid { get; set; }

    public string Professionname { get; set; } = null!;

    public DateTime Createddate { get; set; }

    public bool? Isactive { get; set; }

    public bool? Isdeleted { get; set; }

    public virtual ICollection<Healthprofessional> Healthprofessionals { get; set; } = new List<Healthprofessional>();
}
