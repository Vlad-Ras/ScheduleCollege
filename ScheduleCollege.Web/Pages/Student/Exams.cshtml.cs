using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;

namespace ScheduleCollege.Web.Pages.Student;

[Authorize(Roles = "Student")]
public class ExamsModel : PageModel
{
    private readonly AppDbContext _db;

    public ExamsModel(AppDbContext db)
    {
        _db = db;
    }

    public string GroupCode { get; set; } = "";
    public string Message { get; set; } = "";
    public bool CanViewAllGroups { get; set; }
    public List<ScheduleEntry> Entries { get; set; } = new();

    public async Task OnGetAsync()
    {
        GroupCode = await GetStudentGroupCodeAsync();
        CanViewAllGroups = await GetBoolSettingAsync("StudentCanViewAllGroups", false);

        if (!CanViewAllGroups && string.IsNullOrWhiteSpace(GroupCode))
        {
            Message = "Для вашего пользователя не указана учебная группа. Обратитесь к администратору.";
            return;
        }

        var query = _db.ScheduleEntries
            .Include(x => x.Group)
            .Include(x => x.Subject)
            .Include(x => x.Room)
            .Include(x => x.TimeSlot)
            .Where(x => x.StudyDate.Date >= DateTime.Today)
            .Where(x => x.Subject != null &&
                (x.Subject.Title.ToLower().Contains("экзамен") || x.Subject.Title.ToLower().Contains("зачёт") || x.Subject.Title.ToLower().Contains("зачет")));

        if (!CanViewAllGroups)
        {
            query = query.Where(x => x.Group != null && x.Group.Code == GroupCode);
        }

        Entries = await query
            .OrderBy(x => x.StudyDate)
            .ThenBy(x => x.TimeSlot!.PairNumber)
            .Take(30)
            .ToListAsync();
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
}
