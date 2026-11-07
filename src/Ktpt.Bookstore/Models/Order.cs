using System;
using System.Collections.Generic;

namespace Ktpt.Bookstore.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public DateOnly OrderDate { get; set; }

    public int ClientId { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
