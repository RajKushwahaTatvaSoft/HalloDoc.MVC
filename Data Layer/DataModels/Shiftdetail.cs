using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Shiftdetail
{
    public int Shiftdetailid { get; set; }

    public int Shiftid { get; set; }

    public DateTime Shiftdate { get; set; }

    public int? Regionid { get; set; }

    public TimeOnly Starttime { get; set; }

    public TimeOnly Endtime { get; set; }

    public short Status { get; set; }

    public bool Isdeleted { get; set; }

    public string? Modifiedby { get; set; }

    public DateTime? Modifieddate { get; set; }

    public DateTime? Lastrunningdate { get; set; }

    public string? Eventid { get; set; }

    public bool? Issync { get; set; }

    public virtual Aspnetuser? ModifiedbyNavigation { get; set; }

    public virtual Region? Region { get; set; }

    public virtual Shift Shift { get; set; } = null!;

    public virtual ICollection<Shiftdetailregion> Shiftdetailregions { get; set; } = new List<Shiftdetailregion>();
}
