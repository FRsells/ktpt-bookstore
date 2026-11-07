using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Ktpt.Bookstore.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.Bookstore.Views;

// Вспомогательные методы для интерфейса: цвета, картинки, тексты ошибок.
public static class UiSupport
{
    // Адрес папки Assets внутри программы.
    private const string AssetsRoot = "avares://Ktpt.Bookstore/Assets/";

    // Уже загруженные картинки: ключ — адрес файла, значение — картинка.
    private static readonly Dictionary<string, Bitmap> cache = new();

    // Цвет из App.axaml по имени (x:Key).
    public static IBrush Brush(string key)
    {
        return (IBrush)Application.Current!.Resources[key]!;
    }

    // Обложка из папки Assets/Images по имени файла из столбца image_path.
    // Если имя пустое или такого файла нет — возвращаем заглушку picture.png.
    public static Bitmap Picture(string? fileName)
    {
        string uri = AssetsRoot + "Images/" + fileName;
        if (string.IsNullOrWhiteSpace(fileName) || !AssetLoader.Exists(new Uri(uri)))
        {
            uri = AssetsRoot + "picture.png";
        }

        if (!cache.TryGetValue(uri, out Bitmap? bitmap))
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            bitmap = new Bitmap(stream);
            cache[uri] = bitmap;
        }
        return bitmap;
    }

    // Превращает техническую ошибку в понятный пользователю текст.
    public static string ErrorText(Exception error)
    {
        if (error is AppException)
            return error.Message;

        if (error is SqlException || error is DbUpdateException)
            return "Ошибка базы данных. Проверьте, что SQL Server запущен и строка " +
                   "в connection.txt верна, затем нажмите «Обновить».";

        if (error is IOException)
            return "Не найден файл connection.txt рядом с программой.";

        return "Операция не выполнена: " + error.Message;
    }
}
