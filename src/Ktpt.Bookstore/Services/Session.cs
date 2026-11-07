using Ktpt.Bookstore.Data;
using Ktpt.Bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace Ktpt.Bookstore.Services;

// Session («сеанс») помнит, кто сейчас работает с программой.
public static class Session
{
    // Текущий пользователь. null означает «гость».
    public static User? CurrentUser { get; private set; }

    // Название роли текущего пользователя; для гостя — пустая строка.
    public static string RoleName => CurrentUser?.Role.RoleName ?? "";

    // Уровни доступа (названия ролей — точно как в таблице roles).
    public static bool IsGuest => CurrentUser == null;
    public static bool IsStaff => RoleName == "Продавец" || RoleName == "Администратор";
    public static bool IsAdmin => RoleName == "Администратор";

    // Подпись в шапке окна: «Волков Дмитрий Сергеевич · Покупатель» или «Гость».
    public static string Caption =>
        CurrentUser == null ? "Гость" : $"{CurrentUser.FullName} · {RoleName}";

    // Все пользователи для выпадающего списка входа (вместе с ролями).
    public static async Task<List<User>> GetUsersAsync()
    {
        using var db = Db.Create();
        return await db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Login)
            .AsNoTracking()
            .ToListAsync();
    }

    // Покупатели — для выбора клиента продавцом или администратором.
    public static async Task<List<User>> GetClientsAsync()
    {
        using var db = Db.Create();
        return await db.Users
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "Покупатель")
            .OrderBy(u => u.LastName)
            .AsNoTracking()
            .ToListAsync();
    }

    // Вход: роль заново читается из БД, а не берётся из подписи в списке.
    public static async Task LoginAsync(int userId)
    {
        using var db = Db.Create();
        CurrentUser = await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .SingleAsync(u => u.UserId == userId);
    }

    // Выход: снова гость.
    public static void Logout()
    {
        CurrentUser = null;
    }

    // Проверки прав. Вызываются в сервисах ПЕРЕД изменением данных.
    public static void RequireStaff()
    {
        if (!IsStaff)
            throw new AppException("Недостаточно прав: нужна роль продавца или администратора.");
    }

    public static void RequireAdmin()
    {
        if (!IsAdmin)
            throw new AppException("Недостаточно прав: нужна роль администратора.");
    }
}
