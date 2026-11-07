using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.Bookstore.Models;
using Ktpt.Bookstore.Services;

namespace Ktpt.Bookstore.Views;

public partial class OrderDetailsView : UserControl
{
    // Номер открытого заказа: new OrderDetailsView { OrderId = 7 }
    public int OrderId { get; set; }

    // Что удалить после подтверждения: номер позиции или null (= весь заказ).
    private int? itemToDelete;

    public OrderDetailsView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadOrderAsync();
    }

    private async Task LoadOrderAsync()
    {
        try
        {
            ConfirmPanel.IsVisible = false;
            Order? order = await OrderService.GetOrderAsync(OrderId);
            if (order == null)
            {
                // Заказа больше нет (удалили весь заказ или последнюю позицию).
                MainWindow.Instance.OpenPage(new OrdersView(), clearHistory: true);
                MainWindow.Instance.ShowMessage("Заказ удалён, экземпляры возвращены в остаток.");
                return;
            }

            HeaderText.Text = $"Заказ № {order.OrderId}";
            ClientText.Text = "Покупатель: " + order.Client.FullName;
            DateText.Text = "Дата: " + order.OrderDate.ToString("dd.MM.yyyy");
            TotalText.Text = $"Итого: {order.OrderItems.Sum(i => i.Total):N2} руб.";
            ItemsList.ItemsSource = order.OrderItems.ToList();

            AdminPanel.IsVisible = Session.IsAdmin;
            DateBox.SelectedDate = order.OrderDate.ToDateTime(TimeOnly.MinValue);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.ShowMessage(""); // убрать старое сообщение
        await LoadOrderAsync();
    }

    // Кнопка «Сохранить дату» (администратор).
    private async void SaveDateClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DateBox.SelectedDate == null)
                throw new AppException("Выберите дату.");
            DateOnly date = DateOnly.FromDateTime(DateBox.SelectedDate.Value);
            await OrderService.ChangeDateAsync(OrderId, date);
            await LoadOrderAsync();
            MainWindow.Instance.ShowMessage("Дата сохранена. Цены позиций не изменились.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Удалить весь заказ»: только показываем вопрос.
    private void DeleteOrderClick(object? sender, RoutedEventArgs e)
    {
        itemToDelete = null;
        ConfirmText.Text = "Удалить весь заказ? Все его экземпляры вернутся в остаток.";
        ConfirmPanel.IsVisible = true;
    }

    // Кнопка «Удалить выбранную позицию»: только показываем вопрос.
    private void DeleteItemClick(object? sender, RoutedEventArgs e)
    {
        OrderItem? item = ItemsList.SelectedItem as OrderItem;
        if (item == null)
        {
            MainWindow.Instance.ShowMessage("Сначала выберите позицию в списке.");
            return;
        }
        itemToDelete = item.OrderItemId;
        ConfirmText.Text = "Удалить позицию? Её экземпляры вернутся в остаток. " +
                           "Если позиция последняя, удалится и весь заказ.";
        ConfirmPanel.IsVisible = true;
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        ConfirmPanel.IsVisible = false;
    }

    // Кнопка «Да, удалить».
    private async void ConfirmClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await OrderService.DeleteAsync(OrderId, itemToDelete);
            MainWindow.Instance.ShowMessage("Удаление выполнено, экземпляры возвращены в остаток.");
            await LoadOrderAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }
}
