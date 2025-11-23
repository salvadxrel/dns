using System;
using System.Collections.Generic;

namespace dns.Entities;

public partial class User
{
    public int UsersId { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Mail { get; set; }

    public int RoleId { get; set; }

    public DateOnly CreatedAt { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Comment> Comment { get; } = new List<Comment>();

    public virtual ICollection<Order> Order { get; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;
}
