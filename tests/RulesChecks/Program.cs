using Ktpt.Bookstore.Data;
using Ktpt.Bookstore.Services;
using Microsoft.EntityFrameworkCore;

// Программа проверок: 1) расчётные правила без БД; 2) операции сервисов на учебной БД.
// Запуск: dotnet run (из папки tests\RulesChecks). Каждая строка — PASS или FAIL.

int passed = 0, failed = 0;

void Check(string name, bool ok, string details = "")
{
    if (ok) passed++; else failed++;
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}{(details == "" ? "" : "  → " + details)}");
}

async Task<string?> ErrorOf(Func<Task> action)
{
    try { await action(); return null; }
    catch (AppException ex) { return ex.Message; }
}

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== 1. Расчётные правила (PriceRules) ===");

Check("Остаток 6 → «много»", PriceRules.Availability(6) == "много");
Check("Остаток 5 → «мало»", PriceRules.Availability(5) == "мало");
Check("Остаток 0 → «нет в наличии»", PriceRules.Availability(0) == "нет в наличии");
Check("Остаток 3 → красная карточка", PriceRules.LowStock(3));
Check("Остаток 4 → обычная карточка", !PriceRules.LowStock(4));
Check("Нет заказов в прошлом месяце: 1000 → 750,00", PriceRules.Price(1000m, false) == 750.00m);
Check("Были заказы: 1000 → 1000,00", PriceRules.Price(1000m, true) == 1000.00m);
Check("Округление до копеек: 495 → 371,25", PriceRules.Price(495m, false) == 371.25m);
Check("Округление половины копейки вверх: 0,10 → 0,08", PriceRules.Price(0.10m, false) == 0.08m);

var (s1, e1) = PriceRules.PreviousMonth(new DateOnly(2026, 11, 7));
Check("07.11.2026 → прошлый месяц 01.10.2026–01.11.2026",
    s1 == new DateOnly(2026, 10, 1) && e1 == new DateOnly(2026, 11, 1));
Check("Заказ 01.10.2026 входит в прошлый месяц (скидки нет)",
    new DateOnly(2026, 10, 1) >= s1 && new DateOnly(2026, 10, 1) < e1);
Check("Заказ 01.11.2026 не входит в прошлый месяц",
    !(new DateOnly(2026, 11, 1) >= s1 && new DateOnly(2026, 11, 1) < e1));
var (s2, e2) = PriceRules.PreviousMonth(new DateOnly(2027, 1, 15));
Check("Январь 2027 → декабрь 2026", s2 == new DateOnly(2026, 12, 1) && e2 == new DateOnly(2027, 1, 1));
var (s3, e3) = PriceRules.PreviousMonth(new DateOnly(2028, 3, 1));
Check("01.03.2028 (после 29.02) → 01.02.2028–01.03.2028",
    s3 == new DateOnly(2028, 2, 1) && e3 == new DateOnly(2028, 3, 1));
Check("29.02.2028 входит в февраль 2028", new DateOnly(2028, 2, 29) >= s3 && new DateOnly(2028, 2, 29) < e3);

Console.WriteLine();
Console.WriteLine("=== 2. Операции сервисов на БД bookstore ===");

const int BookId = 9;     // «Дама с собачкой»
const int Client1 = 4;    // client1
const int Client2 = 5;    // client2

using (var db = Db.Create())
{
    int sellerId = (await db.Users.SingleAsync(u => u.Login == "seller1")).UserId;
    int adminId = (await db.Users.SingleAsync(u => u.Login == "admin")).UserId;
    int startStock = (await db.Books.SingleAsync(b => b.BookId == BookId)).StockQuantity;
    int startOrders = await db.Orders.CountAsync();
    var book = await db.Books.AsNoTracking().SingleAsync(b => b.BookId == BookId);
    decimal price = await CatalogService.GetPriceAsync(db, book);

    // Права: продавец не может менять дату и удалять позицию.
    await Session.LoginAsync(sellerId);
    string? err = await ErrorOf(() => OrderService.ChangeDateAsync(2, new DateOnly(2026, 10, 6)));
    Check("Продавец меняет дату через сервис → отказ", err != null, err ?? "");
    err = await ErrorOf(() => OrderService.DeleteAsync(2, 1));
    Check("Продавец удаляет позицию через сервис → отказ", err != null, err ?? "");

    // Покупатель не может оформить заказ на другого покупателя.
    await Session.LoginAsync(Client1);
    err = await ErrorOf(() => OrderService.CreateOrderAsync(BookId, 1, Client2, price));
    Check("Покупатель оформляет заказ на чужое имя → отказ", err != null, err ?? "");

    // Цена изменилась после открытия карточки.
    err = await ErrorOf(() => OrderService.CreateOrderAsync(BookId, 1, Client1, price + 1));
    Check("Цена на форме отличается от текущей → отказ", err != null, err ?? "");
    Check("После отказов заказов не добавилось", await db.Orders.CountAsync() == startOrders);

    // Два покупателя одновременно заказывают последний экземпляр.
    await Session.LoginAsync(adminId);
    await db.Books.Where(b => b.BookId == BookId)
        .ExecuteUpdateAsync(s => s.SetProperty(b => b.StockQuantity, 1));
    var t1 = Task.Run(() => OrderService.CreateOrderAsync(BookId, 1, Client1, price));
    var t2 = Task.Run(() => OrderService.CreateOrderAsync(BookId, 1, Client2, price));
    try { await Task.WhenAll(t1, t2); } catch { }
    int ok = (t1.IsCompletedSuccessfully ? 1 : 0) + (t2.IsCompletedSuccessfully ? 1 : 0);
    int stockAfter = (await db.Books.AsNoTracking().SingleAsync(b => b.BookId == BookId)).StockQuantity;
    string loser = (t1.IsFaulted ? t1.Exception!.InnerException!.Message : t2.IsFaulted ? t2.Exception!.InnerException!.Message : "-");
    Check("Последний экземпляр: один заказ успешен, второй отклонён", ok == 1, loser);
    Check("Остаток после одновременного заказа = 0", stockAfter == 0, $"остаток {stockAfter}");
    int newOrder = t1.IsCompletedSuccessfully ? t1.Result : t2.Result;

    // Две попытки удалить один и тот же заказ: остаток возвращается один раз.
    var d1 = Task.Run(() => OrderService.DeleteAsync(newOrder, null));
    var d2 = Task.Run(() => OrderService.DeleteAsync(newOrder, null));
    try { await Task.WhenAll(d1, d2); } catch { }
    int deleted = (d1.IsCompletedSuccessfully ? 1 : 0) + (d2.IsCompletedSuccessfully ? 1 : 0);
    stockAfter = (await db.Books.AsNoTracking().SingleAsync(b => b.BookId == BookId)).StockQuantity;
    Check("Двойное удаление: выполнено одно удаление", deleted == 1);
    Check("Двойное удаление: остаток вернулся один раз (= 1)", stockAfter == 1, $"остаток {stockAfter}");

    // Возвращаем исходный остаток.
    await db.Books.Where(b => b.BookId == BookId)
        .ExecuteUpdateAsync(s => s.SetProperty(b => b.StockQuantity, startStock));
    Check("Исходные данные восстановлены", await db.Orders.CountAsync() == startOrders);
}

// Замер: те же запросы, что выполняет каталог (книги со связями + цена каждой книги).
Console.WriteLine();
Console.WriteLine("=== 3. Замер загрузки данных каталога ===");
long worst = 0;
for (int run = 1; run <= 5; run++)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    using var db = Db.Create();
    var books = await db.Books.Include(b => b.Genre).Include(b => b.Publisher).Include(b => b.Authors)
        .AsNoTracking().ToListAsync();
    foreach (var b in books)
        await CatalogService.GetPriceAsync(db, b);
    sw.Stop();
    if (run > 1) worst = Math.Max(worst, sw.ElapsedMilliseconds); // 1-й запуск — прогрев
    Console.WriteLine($"Запуск {run}: {books.Count} книг, {sw.ElapsedMilliseconds} мс");
}
Check("Загрузка данных каталога не дольше 1 с (без первого запуска)", worst <= 1000, $"максимум {worst} мс");

Console.WriteLine();
Console.WriteLine($"Итого: пройдено {passed}, не пройдено {failed}");
return failed == 0 ? 0 : 1;
