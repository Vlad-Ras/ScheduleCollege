using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;

namespace ScheduleCollege.Web.Pages.Student;

[Authorize(Roles = "Student")]
public class ScheduleModel : PageModel
{
    private readonly AppDbContext _db;

    public ScheduleModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime WeekDate { get; set; } = DateTime.Today;

    [BindProperty(SupportsGet = true)]
    public string? SelectedGroupCode { get; set; }

    public string GroupCode { get; set; } = "";
    public string Message { get; set; } = "";
    public bool CanViewAllGroups { get; set; }
    public DateTime WeekStart { get; set; }
    public List<DateTime> WeekDays { get; set; } = new();
    public List<int> PairNumbers { get; set; } = new();
    public List<StudentGroup> Groups { get; set; } = new();
    public List<ScheduleEntry> Entries { get; set; } = new();

    public async Task OnGetAsync()
    {
        GroupCode = await GetStudentGroupCodeAsync();
        CanViewAllGroups = await GetBoolSettingAsync("StudentCanViewAllGroups", false);
        Groups = await _db.Groups.AsNoTracking().OrderBy(x => x.Code).ToListAsync();

        if (!CanViewAllGroups && string.IsNullOrWhiteSpace(GroupCode))
        {
            Message = "Для вашего пользователя не указана учебная группа. Обратитесь к администратору.";
            return;
        }

        WeekStart = GetWeekStart(WeekDate);
        WeekDays = Enumerable.Range(0, 7).Select(x => WeekStart.AddDays(x)).ToList();
        var weekEnd = WeekStart.AddDays(7);

        PairNumbers = await _db.TimeSlots
            .Select(x => x.PairNumber)
            .Distinct()
            .Where(x => x >= 1 && x <= 6)
            .OrderBy(x => x)
            .ToListAsync();

        var query = _db.ScheduleEntries
            .Include(x => x.Group)
            .Include(x => x.Subject)
            .Include(x => x.Room)
            .Include(x => x.TimeSlot)
            .Where(x => x.StudyDate >= WeekStart && x.StudyDate < weekEnd);

        if (CanViewAllGroups)
        {
            if (!string.IsNullOrWhiteSpace(SelectedGroupCode))
            {
                query = query.Where(x => x.Group != null && x.Group.Code == SelectedGroupCode);
            }
        }
        else
        {
            query = query.Where(x => x.Group != null && x.Group.Code == GroupCode);
        }

        Entries = await query
            .OrderBy(x => x.StudyDate)
            .ThenBy(x => x.TimeSlot!.PairNumber)
            .ThenBy(x => x.Group!.Code)
            .ToListAsync();
    }

    public List<ScheduleEntry> GetLessons(DateTime date, int pairNumber)
    {
        return Entries
            .Where(x => x.StudyDate.Date == date.Date && x.TimeSlot?.PairNumber == pairNumber)
            .ToList();
    }

    public string GetRoomText(Room? room)
    {
        if (room == null)
        {
            return "";
        }

        if (room.Format == "Онлайн" && !string.IsNullOrWhiteSpace(room.OnlineLink))
        {
            return $"{room.Number} ({room.OnlineLink})";
        }

        return room.Format == "Онлайн" ? $"{room.Number} (онлайн)" : room.Number;
    }

    public static string GetShortDayName(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Пн",
            DayOfWeek.Tuesday => "Вт",
            DayOfWeek.Wednesday => "Ср",
            DayOfWeek.Thursday => "Чт",
            DayOfWeek.Friday => "Пт",
            DayOfWeek.Saturday => "Сб",
            DayOfWeek.Sunday => "Вс",
            _ => ""
        };
    }

    private async Task<string> GetStudentGroupCodeAsync()
    {
        var idText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idText, out var id))
        {
            var user = await _db.Users.FindAsync(id);
            if (!string.IsNullOrWhiteSpace(user?.GroupCode))
            {
                return user.GroupCode.Trim();
            }
        }

        return "";
    }

    private async Task<bool> GetBoolSettingAsync(string key, bool defaultValue)
    {
        var value = await _db.SystemSettings
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }
}
