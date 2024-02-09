using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Shiftdetailregion
{
    public int Shiftdetailregionid { get; set; }

    public int Shiftdetailid { get; set; }

    public int Regionid { get; set; }

    public bool? Isdeleted { get; set; }

    public virtual Region Region { get; set; } = null!;

    public virtual Shiftdetail Shiftdetail { get; set; } = null!;
}
