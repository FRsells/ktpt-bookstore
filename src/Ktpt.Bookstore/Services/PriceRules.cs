namespace Ktpt.Bookstore.Services;

// Правила расчёта (БП-1, БП-2). Здесь нет ни базы данных, ни окон — только арифметика,
// поэтому методы проверяются отдельной программой tests/RulesChecks.
public static class PriceRules
{
    // Границы предыдущего календарного месяца для даты day.
    // Пример: day = 07.11.2026 → Start = 01.10.2026, End = 01.11.2026 (End не входит).
    public static (DateOnly Start, DateOnly End) PreviousMonth(DateOnly day)
    {
        DateOnly end = new DateOnly(day.Year, day.Month, 1);
        DateOnly start = end.AddMonths(-1);
        return (start, end);
    }

    // БП-1. Цена экземпляра: скидка 25 %, если в прошлом месяце книгу не заказывали.
    public static decimal Price(decimal basePrice, bool hadOrders)
    {
        decimal price = hadOrders ? basePrice : basePrice * 0.75m;
        return decimal.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    // БП-2. Текст о наличии: больше 5 — «много», от 1 до 5 — «мало», 0 — «нет в наличии».
    public static string Availability(int stock)
    {
        if (stock > 5)
            return "много";
        if (stock > 0)
            return "мало";
        return "нет в наличии";
    }

    // БП-2. Красная подсветка карточки: экземпляров 3 или меньше.
    public static bool LowStock(int stock) => stock <= 3;
}
