using System;
using System.Collections.Generic;

namespace dns.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Product> Product { get; } = new List<Product>();
}
