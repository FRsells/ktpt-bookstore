using Microsoft.EntityFrameworkCore;

namespace Ktpt.Bookstore.Data;

// Db — «фабрика подключений». В любом месте программы пишем:
// using var db = Db.Create();
// и получаем готовый к работе BookstoreContext (его создал scaffold).
public static class Db
{
    public static BookstoreContext Create()
    {
        // connection.txt копируется рядом с .exe (настройка в .csproj).
        string path = Path.Combine(AppContext.BaseDirectory, "connection.txt");

        // Читаем строку подключения; Trim убирает лишние пробелы и переводы строк.
        string connection = File.ReadAllText(path).Trim();

        // Работаем с SQL Server по этой строке подключения.
        var options = new DbContextOptionsBuilder<BookstoreContext>()
            .UseSqlServer(connection)
            .Options;

        return new BookstoreContext(options);
    }
}
