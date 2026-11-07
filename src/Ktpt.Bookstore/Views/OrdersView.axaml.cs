using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.Bookstore.Models;
using Ktpt.Bookstore.Services;

namespace Ktpt.Bookstore.Views;

public partial class OrdersView : UserControl
{
    public OrdersView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            OrdersList.ItemsSource = await OrderService.GetOrdersAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.ShowMessage(""); // убрать старое сообщение
        await LoadOrdersAsync();
    }

    // Кнопка «Новый заказ»: в каталоге выбираем книгу, затем покупателя.
    private void NewClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.OpenPage(new CatalogView());
    }

    // Кнопка «Состав заказа».
    private void OpenClick(object? sender, RoutedEventArgs e)
    {
        Order? order = OrdersList.SelectedItem as Order;
        if (order == null)
        {
            MainWindow.Instance.ShowMessage("Сначала выберите заказ в списке.");
            return;
        }
        MainWindow.Instance.OpenPage(new OrderDetailsView { OrderId = order.OrderId });
    }
}
