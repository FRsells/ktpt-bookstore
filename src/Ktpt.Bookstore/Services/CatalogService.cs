using Ktpt.Bookstore.Data;
using Ktpt.Bookstore.Models;
using Ktpt.Bookstore.Views;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.Bookstore.Services;

// Чтение данных для каталога и расчёт текущей цены книги.
public static class CatalogService
{
    // Текущая цена одной книги: со скидкой или без (правило — в PriceRules).
    public static async Task<decimal> GetPriceAsync(BookstoreContext db, Book book)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        var (start, end) = PriceRules.PreviousMonth(today);

        // Есть ли хотя бы одна позиция заказа этой книги в прошлом месяце?
        bool hadOrders = await db.OrderItems.AnyAsync(i =>
            i.BookId == book.BookId &&
            i.Order.OrderDate >= start &&
            i.Order.OrderDate < end);

        return PriceRules.Price(book.BasePrice, hadOrders);
    }

    // Авторы книги одной строкой: «Ильф И. А., Петров Е. П.».
    public static string AuthorsText(Book book) =>
        string.Join(", ", book.Authors.OrderBy(a => a.LastName).Select(a => a.ShortName));

    // Все карточки каталога.
    public static async Task<List<BookCard>> LoadCardsAsync()
    {
        using var db = Db.Create();

        List<Book> books = await db.Books
            .Include(b => b.Genre)
            .Include(b => b.Publisher)
            .Include(b => b.Authors)
            .AsNoTracking()
            .ToListAsync();

        var cards = new List<BookCard>();
        foreach (Book b in books)
        {
            decimal price = await GetPriceAsync(db, b);
            bool discount = price < b.BasePrice;

            cards.Add(new BookCard
            {
                Id = b.BookId,
                Title = b.Title,
                Authors = AuthorsText(b),
                Isbn = b.Isbn,
                Genre = b.Genre.GenreName,
                Publisher = $"{b.Publisher.PublisherName}, {b.PublicationYear} г.",
                Stock = b.StockQuantity,
                StockText = "Наличие: " + PriceRules.Availability(b.StockQuantity),
                Price = price,
                PriceText = $"{price:N2} руб.",
                BasePriceText = discount ? $"Базовая цена: {b.BasePrice:N2} руб." : "",
                DiscountText = discount ? "Скидка 25 %" : "",
                Background = UiSupport.Brush(PriceRules.LowStock(b.StockQuantity)
                    ? "LowStockBrush" : "NormalBrush"),
                Image = UiSupport.Picture(b.ImagePath)
            });
        }
        return cards;
    }
}
