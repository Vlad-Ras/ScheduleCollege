using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Teacher;

[Authorize(Roles = "Teacher")]
public class AvailabilityModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public AvailabilityModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public List<string> AllDays { get; set; } = new()
    {
        "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"
    };

    [BindProperty]
    public List<string> SelectedDays { get; set; } = new();

    public string TeacherName { get; set; } = "";
    public string Message { get; set; } = "";
    public string ErrorMessage { get; set; } = "";

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var teacher = await FindCurrentTeacherAsync();
        if (teacher == null)
        {
            ErrorMessage = "Ваш пользователь не связан со справочником преподавателей. Проверьте, что ФИО пользователя совпадает с ФИО преподавателя.";
            await LoadAsync();
            return Page();
        }

        var cleanDays = SelectedDays
            .Where(x => AllDays.Contains(x))
            .Distinct()
            .ToList();

        teacher.UnavailableDays = string.Join(", ", cleanDays);
        await _db.SaveChangesAsync();
        await _audit.AddAsync("Изменение занятых дней преподавателя", teacher.FullName);

        Message = "Занятые дни сохранены.";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        var teacher = await FindCurrentTeacherAsync();
        if (teacher == null)
        {
            TeacherName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Преподаватель";
            ErrorMessage = "Ваш пользователь не связан со справочником преподавателей. Проверьте, что ФИО пользователя совпадает с ФИО преподавателя.";
            SelectedDays = new List<string>();
            return;
        }

        TeacherName = teacher.FullName;
        SelectedDays = teacher.UnavailableDays
            .Split(',', ';')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private async Task<ScheduleCollege.Web.Models.Teacher?> FindCurrentTeacherAsync()
    {
        var fullName = User.FindFirst("FullName")?.Value ?? "";
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        return await _db.Teachers.FirstOrDefaultAsync(x => x.FullName == fullName);
    }
}
