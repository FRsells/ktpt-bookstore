namespace Ktpt.Bookstore.Models;

// Дополнения к классам, созданным scaffold. Сами созданные файлы не редактируем.

public partial class User
{
    // ФИО одной строкой. Trim убирает пробел в конце, если отчества нет.
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

    // Так пользователь выглядит в выпадающем списке.
    public override string ToString() => $"{Login} — {FullName}";
}

public partial class Author
{
    // Фамилия и инициалы: «Пушкин А. С.».
    public string ShortName =>
        $"{LastName} {FirstName[0]}." + (string.IsNullOrEmpty(MiddleName) ? "" : $" {MiddleName[0]}.");
}

public partial class Order
{
    // Так заказ выглядит в списке заказов: номер, дата, ФИО покупателя.
    public override string ToString() =>
        $"Заказ № {OrderId} · {OrderDate:dd.MM.yyyy} · {Client?.FullName}";
}

public partial class OrderItem
{
    // Стоимость позиции: количество × цена покупки.
    public decimal Total => Quantity * UnitPrice;

    // Так позиция выглядит в составе заказа.
    public override string ToString() =>
        $"{Book?.Title} — {Quantity} шт. × {UnitPrice:N2} = {Total:N2} руб.";
}
