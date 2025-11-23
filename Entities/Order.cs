using System;
using System.Collections.Generic;

namespace dns.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int UsersId { get; set; }

    public DateOnly OrderDate { get; set; }

    public int OrderStatusId { get; set; }

    public string Address { get; set; } = null!;

    public virtual ICollection<OrderProduct> OrderProduct { get; } = new List<OrderProduct>();

    public virtual StatusOrder OrderStatus { get; set; } = null!;

    public virtual User Users { get; set; } = null!;
}
