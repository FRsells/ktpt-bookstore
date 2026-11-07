using System;
using System.Collections.Generic;

namespace Ktpt.Bookstore.Models;

public partial class Book
{
    public int BookId { get; set; }

    public string Isbn { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int GenreId { get; set; }

    public int PublisherId { get; set; }

    public short PublicationYear { get; set; }

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public decimal BasePrice { get; set; }

    public int StockQuantity { get; set; }

    public virtual Genre Genre { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Publisher Publisher { get; set; } = null!;

    public virtual ICollection<Author> Authors { get; set; } = new List<Author>();
}
