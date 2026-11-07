using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Ktpt.Bookstore.Services;

// Одна карточка каталога: всё, что нужно показать на экране для одной книги.
// Разметка CatalogView.axaml берёт значения отсюда через {Binding ИмяСвойства}.
public class BookCard
{
    public int Id { get; set; }                     // book_id — чтобы открыть книгу
    public string Title { get; set; } = "";         // название
    public string Authors { get; set; } = "";       // авторы через запятую
    public string Isbn { get; set; } = "";          // ISBN (для поиска)
    public string Genre { get; set; } = "";         // жанр
    public string Publisher { get; set; } = "";     // издательство и год
    public int Stock { get; set; }                  // остаток
    public string StockText { get; set; } = "";     // «Наличие: много»
    public decimal Price { get; set; }              // итоговая цена (для сортировки)
    public string PriceText { get; set; } = "";     // «337,50 руб.»
    public string BasePriceText { get; set; } = ""; // «Базовая цена: 450,00 руб.»
    public string DiscountText { get; set; } = "";  // «Скидка 25 %» или пусто
    public IBrush? Background { get; set; }         // фон карточки (красный, если мало)
    public Bitmap? Image { get; set; }              // обложка или заглушка
}
