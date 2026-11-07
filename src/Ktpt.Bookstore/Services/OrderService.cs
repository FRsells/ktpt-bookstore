using Ktpt.Bookstore.Data;
using Ktpt.Bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.Bookstore.Services;

// Все операции с заказами: оформить, показать, изменить дату, удалить.
public static class OrderService
{
    // ---------- 1. Оформление заказа (БП-3, БП-4). Возвращает номер нового заказа. ----------
    public static async Task<int> CreateOrderAsync(int bookId, int quantity,
        int clientId, decimal shownPrice)
    {
        if (Session.CurrentUser == null)
            throw new AppException("Гость не может оформить заказ. Войдите в систему.");
        if (!Session.IsStaff && clientId != Session.CurrentUser.UserId)
            throw new AppException("Можно оформить заказ только на себя.");
        if (quantity <= 0)
            throw new AppException("Количество экземпляров должно быть больше нуля.");

        using var db = Db.Create();

        // Транзакция: всё, что ниже, сохранится ВМЕСТЕ или не сохранится ВООБЩЕ.
        using var transaction = await db.Database.BeginTransactionAsync();

        Book? book = await db.Books
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.BookId == bookId);
        if (book == null)
            throw new AppException("Книга не найдена. Обновите каталог.");

        // Пересчитываем цену: вдруг она изменилась, пока форма была открыта.
        decimal price = await CatalogService.GetPriceAsync(db, book);
        if (price != shownPrice)
            throw new AppException("Цена изменилась. Нажмите «Обновить карточку».");

        // Уменьшаем остаток ОДНОЙ командой SQL и только если экземпляров хватает:
        // UPDATE books SET stock_quantity = stock_quantity - quantity
        // WHERE book_id = ... AND stock_quantity >= quantity
        int changed = await db.Books
            .Where(b => b.BookId == bookId && b.StockQuantity >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(
                b => b.StockQuantity,
                b => b.StockQuantity - quantity));
        if (changed == 0)
            throw new AppException("Недостаточно экземпляров в наличии. Обновите карточку.");

        var order = new Order
        {
            OrderDate = DateOnly.FromDateTime(DateTime.Today),
            ClientId = clientId
        };
        order.OrderItems.Add(new OrderItem
        {
            BookId = bookId,
            Quantity = quantity,
            UnitPrice = price // цена покупки сохраняется в позиции
        });
        db.Orders.Add(order);

        await db.SaveChangesAsync();      // INSERT в orders, затем в order_items
        await transaction.CommitAsync();  // заказ и списание остатка — вместе
        return order.OrderId;
    }

    // ---------- 2. Список всех заказов (для продавца и администратора). ----------
    public static async Task<List<Order>> GetOrdersAsync()
    {
        Session.RequireStaff();
        using var db = Db.Create();
        return await db.Orders
            .Include(o => o.Client)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.OrderId)
            .AsNoTracking()
            .ToListAsync();
    }

    // ---------- 3. Один заказ со всем составом. null — если заказа уже нет. ----------
    public static async Task<Order?> GetOrderAsync(int orderId)
    {
        Session.RequireStaff();
        using var db = Db.Create();
        return await db.Orders
            .Include(o => o.Client)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Book)
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.OrderId == orderId);
    }

    // ---------- 4. Изменить дату заказа (БП-6, только администратор). ----------
    public static async Task ChangeDateAsync(int orderId, DateOnly newDate)
    {
        Session.RequireAdmin();
        using var db = Db.Create();
        Order? order = await db.Orders.FindAsync(orderId);
        if (order == null)
            throw new AppException("Заказ уже удалён. Обновите список.");
        order.OrderDate = newDate;
        await db.SaveChangesAsync();
    }

    // ---------- 5. Удаление (БП-5, БП-6) ----------
    // itemId = номер позиции → удалить одну позицию (только администратор);
    // itemId = null → удалить весь заказ (продавец или администратор).
    public static async Task DeleteAsync(int orderId, int? itemId)
    {
        if (itemId == null)
            Session.RequireStaff();
        else
            Session.RequireAdmin();

        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync();

        List<OrderItem> items = await db.OrderItems
            .Where(i => i.OrderId == orderId && (itemId == null || i.OrderItemId == itemId))
            .ToListAsync();
        if (items.Count == 0)
            throw new AppException("Эти данные уже удалены. Обновите список.");

        // Возвращаем экземпляры: прибавляем количество к текущему остатку в базе.
        foreach (OrderItem item in items)
        {
            await db.Books
                .Where(b => b.BookId == item.BookId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    b => b.StockQuantity,
                    b => b.StockQuantity + item.Quantity));
        }

        db.OrderItems.RemoveRange(items);
        await db.SaveChangesAsync();

        // Если в заказе не осталось позиций — удаляем и сам заказ.
        bool hasItems = await db.OrderItems.AnyAsync(i => i.OrderId == orderId);
        if (!hasItems)
            await db.Orders.Where(o => o.OrderId == orderId).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }
}
