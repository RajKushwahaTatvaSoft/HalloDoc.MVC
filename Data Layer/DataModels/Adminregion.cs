using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Adminregion
{
    public int Adminregionid { get; set; }

    public int? Regionid { get; set; }

    public int? Adminid { get; set; }

    public virtual Admin? Admin { get; set; }

    public virtual Region? Region { get; set; }
}
