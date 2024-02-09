using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Physiciannotification
{
    public int Id { get; set; }

    public int Physicianid { get; set; }

    public bool Isnotificationstopped { get; set; }

    public virtual Physician Physician { get; set; } = null!;
}
