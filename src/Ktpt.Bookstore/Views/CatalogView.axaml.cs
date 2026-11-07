using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.Bookstore.Services;

namespace Ktpt.Bookstore.Views;

public partial class CatalogView : UserControl
{
    // ВСЕ карточки, загруженные из БД. Фильтры каждый раз начинают с полного списка.
    private List<BookCard> allCards = new();

    public CatalogView()
    {
        InitializeComponent();
        SortBox.SelectedIndex = 0; // по умолчанию — «Цена по возрастанию»
        Loaded += async (s, e) => await LoadCardsAsync();
    }

    // Загрузить карточки из БД и заполнить список жанров.
    private async Task LoadCardsAsync()
    {
        try
        {
            string? oldGenre = GenreBox.SelectedItem as string;
            allCards = await CatalogService.LoadCardsAsync();

            var genres = new List<string> { "Все жанры" };
            genres.AddRange(allCards.Select(c => c.Genre).Distinct().OrderBy(g => g));
            GenreBox.ItemsSource = genres;

            if (oldGenre != null && genres.Contains(oldGenre))
                GenreBox.SelectedItem = oldGenre;
            else
                GenreBox.SelectedIndex = 0;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Поиск + жанр + сортировка. Вызывается при любом изменении фильтров.
    private void ApplyFilters()
    {
        string text = (SearchBox.Text ?? "").Trim();

        // 1) Поиск: название, автор или ISBN содержит текст (регистр не важен).
        IEnumerable<BookCard> rows = allCards.Where(c =>
            c.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            c.Authors.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            c.Isbn.Contains(text, StringComparison.OrdinalIgnoreCase));

        // 2) Жанр: индекс 0 — «Все жанры», тогда не фильтруем.
        if (GenreBox.SelectedIndex > 0)
        {
            string genre = (string)GenreBox.SelectedItem!;
            rows = rows.Where(c => c.Genre == genre);
        }

        // 3) Сортировка по цене: индекс 1 — по убыванию, иначе по возрастанию.
        if (SortBox.SelectedIndex == 1)
            rows = rows.OrderByDescending(c => c.Price);
        else
            rows = rows.OrderBy(c => c.Price);

        // 4) Показать результат.
        List<BookCard> result = rows.ToList();
        CardsList.ItemsSource = result;
        CountText.Text = $"Найдено: {result.Count} из {allCards.Count}";
    }

    private void SearchChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void FilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.ShowMessage(""); // убрать старое сообщение
        await LoadCardsAsync();
    }

    // Пользователь выбрал карточку — открываем страницу книги.
    private void CardSelected(object? sender, SelectionChangedEventArgs e)
    {
        BookCard? card = CardsList.SelectedItem as BookCard;
        if (card == null)
            return;

        if (Session.IsGuest)
        {
            CardsList.SelectedItem = null;
            MainWindow.Instance.ShowMessage("Чтобы открыть карточку и оформить заказ, войдите в систему.");
            return;
        }

        MainWindow.Instance.OpenPage(new BookView { BookId = card.Id });
    }
}
