using Avalonia.Controls;
using Avalonia.Interactivity;
using Ktpt.Bookstore.Models;
using Ktpt.Bookstore.Services;

namespace Ktpt.Bookstore.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        // Когда страница появилась на экране — загружаем пользователей из БД.
        Loaded += async (s, e) => await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            UsersBox.ItemsSource = await Session.GetUsersAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Обновить список».
    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.ShowMessage(""); // убрать старое сообщение
        await LoadUsersAsync();
    }

    // Кнопка «Войти».
    private async void LoginClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            User? user = UsersBox.SelectedItem as User;
            if (user == null)
                throw new AppException("Выберите логин из списка.");

            await Session.LoginAsync(user.UserId);
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Войти как гость».
    private void GuestClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();
        MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
    }
}
