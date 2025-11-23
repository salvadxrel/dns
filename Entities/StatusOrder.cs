using System;
using System.Collections.Generic;

namespace dns.Entities;

public partial class StatusOrder
{
    public int StatusOrderId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Order> Order { get; } = new List<Order>();
}
