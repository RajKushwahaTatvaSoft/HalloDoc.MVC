using System;
using System.Collections.Generic;

namespace Data_Layer.DataModels;

public partial class Region
{
    public int Regionid { get; set; }

    public string Name { get; set; } = null!;

    public string? Abbreviation { get; set; }

    public virtual ICollection<Adminregion> Adminregions { get; set; } = new List<Adminregion>();

    public virtual ICollection<Business> Businesses { get; set; } = new List<Business>();

    public virtual ICollection<Concierge> Concierges { get; set; } = new List<Concierge>();

    public virtual ICollection<Physicianregion> Physicianregions { get; set; } = new List<Physicianregion>();

    public virtual ICollection<Requestclient> Requestclients { get; set; } = new List<Requestclient>();

    public virtual ICollection<Shiftdetailregion> Shiftdetailregions { get; set; } = new List<Shiftdetailregion>();

    public virtual ICollection<Shiftdetail> Shiftdetails { get; set; } = new List<Shiftdetail>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
