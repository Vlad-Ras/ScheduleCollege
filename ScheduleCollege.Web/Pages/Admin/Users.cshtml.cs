using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public UsersModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public List<User> Users { get; set; } = new();
    public List<StudentGroup> Groups { get; set; } = new();

    [TempData] public string? Message { get; set; }

    public static string GetRoleText(string role)
    {
        return role switch
        {
            "Admin" => "Администратор",
            "Dispatcher" => "Диспетчер расписания",
            "Teacher" => "Преподаватель",
            "Student" => "Студент",
            _ => role
        };
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Users = await _db.Users.OrderBy(x => x.Login).ToListAsync();
        Groups = await _db.Groups.OrderBy(x => x.Code).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(string login, string fullName, string role, string password, string? groupCode)
    {
        _db.Users.Add(new User
        {
            Login = login,
            FullName = fullName,
            Role = role,
            GroupCode = role == "Student" ? (groupCode ?? "").Trim() : "",
            PasswordHash = PasswordService.CreateHash(password),
            IsActive = true
        });

        await _db.SaveChangesAsync();
        await _audit.AddAsync("Создание пользователя", login);
        Message = "Пользователь добавлен.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetGroupAsync(int id, string? groupCode)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null && user.Role == "Student")
        {
            user.GroupCode = (groupCode ?? "").Trim();
            await _db.SaveChangesAsync();
            await _audit.AddAsync("Назначение группы студенту", $"{user.Login}: {user.GroupCode}");
            Message = "Группа студента сохранена.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null && user.Login != "admin")
        {
            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            await _audit.AddAsync("Смена активности пользователя", user.Login);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null && user.Login != "admin")
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            await _audit.AddAsync("Удаление пользователя", user.Login);
        }

        return RedirectToPage();
    }
}
