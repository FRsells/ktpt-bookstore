using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Ktpt.Bookstore.Services;

namespace Ktpt.Bookstore.Views;

public partial class MainWindow : Window
{
    // Ссылка на единственное главное окно: MainWindow.Instance.OpenPage(...)
    public static MainWindow Instance { get; private set; } = null!;

    // История открытых страниц для кнопки «Назад» (последняя — сверху).
    private readonly Stack<UserControl> history = new();

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        OpenPage(new LoginView(), clearHistory: true); // первая страница — вход
    }

    // Открыть страницу. clearHistory: true — начать историю заново (вход, выход).
    public void OpenPage(UserControl page, bool clearHistory = false)
    {
        if (clearHistory)
        {
            history.Clear();
        }
        else
        {
            UserControl? current = PageHost.Content as UserControl;
            if (current != null)
                history.Push(current);
        }
        ShowPage(page);
    }

    // Показать страницу и обновить шапку и кнопки.
    private void ShowPage(UserControl page)
    {
        PageHost.Content = page;
        Title = "Книжный магазин — " + page.Tag;
        MessageText.Text = "";
        UserText.Text = Session.Caption;
        OrdersButton.IsVisible = Session.IsStaff;  // «Заказы» — только сотрудникам
        BackButton.IsEnabled = history.Count > 0;
    }

    // Обычное сообщение.
    public void ShowMessage(string text)
    {
        MessageText.Foreground = Brushes.Black;
        MessageText.Text = text;
    }

    // Сообщение об ошибке (тёмно-красным цветом).
    public void ShowError(Exception error)
    {
        System.Diagnostics.Trace.WriteLine(error);
        MessageText.Foreground = Brushes.DarkRed;
        MessageText.Text = UiSupport.ErrorText(error);
    }

    // ---------- Обработчики кнопок навигации ----------
    private void BackClick(object? sender, RoutedEventArgs e)
    {
        if (history.Count > 0)
            ShowPage(history.Pop());
    }

    private void CatalogClick(object? sender, RoutedEventArgs e)
    {
        OpenPage(new CatalogView(), clearHistory: true);
    }

    private void OrdersClick(object? sender, RoutedEventArgs e)
    {
        OpenPage(new OrdersView());
    }

    private void LogoutClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();
        OpenPage(new LoginView(), clearHistory: true);
    }
}
