namespace Ktpt.Bookstore.Services;

// Ошибка «для пользователя»: её текст можно сразу показать на экране.
public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }
}
