using System;
using System.Collections.Generic;

namespace dns.Entities;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Amount { get; set; }

    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Comment> Comment { get; } = new List<Comment>();

    public virtual ICollection<OrderProduct> OrderProduct { get; } = new List<OrderProduct>();
}
