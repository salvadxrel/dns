using System;
using System.Collections.Generic;

namespace dns.Entities;

public partial class Role
{
    public int RoleId { get; set; }

    public string Name { get; set; } = null!;

    public string? Discription { get; set; }

    public virtual ICollection<User> User { get; } = new List<User>();
}
