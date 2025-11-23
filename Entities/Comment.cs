using System;
using System.Collections.Generic;

namespace dns.Entities;

public partial class Comment
{
    public int CommentId { get; set; }

    public int UsersId { get; set; }

    public int ProductId { get; set; }

    public DateOnly Date { get; set; }

    public int? Rating { get; set; }

    public string Description { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual User Users { get; set; } = null!;
}
