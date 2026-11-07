using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.Bookstore.Data;
using Ktpt.Bookstore.Models;
using Ktpt.Bookstore.Services;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.Bookstore.Views;

public partial class BookView : UserControl
{
    // Номер открытой книги: new BookView { BookId = 5 }
    public int BookId { get; set; }

    // Цена, которую увидел пользователь. При сохранении сравним её с текущей.
    private decimal shownPrice;

    public BookView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadBookAsync();
    }

    // Загрузить книгу и (для сотрудника) список покупателей.
    private async Task LoadBookAsync()
    {
        try
        {
            SaveButton.IsEnabled = false; // пока данные не загружены — не оформляем
            using var db = Db.Create();

            Book? book = await db.Books
                .Include(b => b.Genre)
                .Include(b => b.Publisher)
                .Include(b => b.Authors)
                .AsNoTracking()
                .SingleOrDefaultAsync(b => b.BookId == BookId);
            if (book == null)
                throw new AppException("Книга не найдена. Вернитесь в каталог.");

            TitleText.Text = book.Title;
            AuthorsText.Text = (book.Authors.Count > 1 ? "Авторы: " : "Автор: ") +
                               CatalogService.AuthorsText(book);
            InfoText.Text = $"Жанр: {book.Genre.GenreName}. Издательство: {book.Publisher.PublisherName}, " +
                            $"{book.PublicationYear} г. ISBN {book.Isbn}";
            DescriptionText.Text = book.Description ?? "Описание отсутствует.";
            BookImage.Source = UiSupport.Picture(book.ImagePath);

            shownPrice = await CatalogService.GetPriceAsync(db, book);
            BasePriceText.Text = $"Базовая цена: {book.BasePrice:N2} руб.";
            PriceText.Text = shownPrice < book.BasePrice
                ? $"Итоговая цена: {shownPrice:N2} руб. (скидка 25 %)"
                : $"Итоговая цена: {shownPrice:N2} руб.";
            StockText.Text = $"В наличии: {book.StockQuantity} шт.";

            // Выбор покупателя — только для продавца и администратора.
            ClientPanel.IsVisible = Session.IsStaff;
            if (Session.IsStaff)
                ClientBox.ItemsSource = await Session.GetClientsAsync();

            SaveButton.IsEnabled = book.StockQuantity > 0;
            if (book.StockQuantity == 0)
                MainWindow.Instance.ShowMessage("Книги нет в наличии, оформить заказ нельзя.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Оформить заказ».
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false; // защита от двойного щелчка
        try
        {
            // 1) Количество: TryParse не «роняет» программу, если ввели буквы.
            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
                throw new AppException("Количество должно быть целым числом больше нуля.");

            // 2) Покупатель: обычный пользователь — сам, сотрудник — выбранный в списке.
            int clientId = Session.CurrentUser!.UserId;
            if (Session.IsStaff)
            {
                User? client = ClientBox.SelectedItem as User;
                if (client == null)
                    throw new AppException("Выберите покупателя.");
                clientId = client.UserId;
            }

            // 3) Сохранение. Проверки остатка и цены — внутри OrderService.
            int orderId = await OrderService.CreateOrderAsync(BookId, quantity, clientId, shownPrice);

            // 4) Успех: открываем свежий каталог и показываем номер заказа.
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
            MainWindow.Instance.ShowMessage($"Заказ № {orderId} оформлен. Каталог обновлён.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
            SaveButton.IsEnabled = true; // ошибка — можно исправить и нажать снова
        }
    }

    // Кнопка «Обновить карточку».
    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.ShowMessage(""); // убрать старое сообщение
        await LoadBookAsync();
    }
}
