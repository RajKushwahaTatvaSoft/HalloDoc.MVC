using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Aspnetuserrole
{
    public string Userid { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual Aspnetuser User { get; set; } = null!;
}
