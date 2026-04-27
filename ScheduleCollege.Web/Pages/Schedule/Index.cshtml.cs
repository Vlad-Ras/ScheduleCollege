using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Schedule;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public IndexModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty(SupportsGet = true)] public int? GroupId { get; set; }
    [BindProperty(SupportsGet = true)] public int? TeacherId { get; set; }
    [BindProperty(SupportsGet = true)] public int? RoomId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? StudyDate { get; set; }

    public List<ScheduleEntry> Entries { get; set; } = new();
    public List<ScheduleEntry> CalendarEntries { get; set; } = new();
    public List<StudentGroup> Groups { get; set; } = new();
    public List<global::ScheduleCollege.Web.Models.Teacher> Teachers { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<int> PairNumbers { get; set; } = new();
    public List<DateTime> WeekDays { get; set; } = new();
    public DateTime WeekStart { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDictionariesAsync();

        WeekStart = GetWeekStart(StudyDate ?? DateTime.Today);
        WeekDays = Enumerable.Range(0, 7).Select(x => WeekStart.AddDays(x)).ToList();

        var query = BuildBaseQuery();

        if (StudyDate.HasValue)
        {
            query = query.Where(x => x.StudyDate.Date == StudyDate.Value.Date);
        }

        Entries = await query
            .OrderBy(x => x.StudyDate)
            .ThenBy(x => x.TimeSlot!.PairNumber)
            .ThenBy(x => x.Group!.Code)
            .ToListAsync();

        var weekEnd = WeekStart.AddDays(7);
        CalendarEntries = await BuildBaseQuery()
            .Where(x => x.StudyDate >= WeekStart && x.StudyDate < weekEnd)
            .OrderBy(x => x.StudyDate)
            .ThenBy(x => x.TimeSlot!.PairNumber)
            .ThenBy(x => x.Group!.Code)
            .ToListAsync();

        PairNumbers = await _db.TimeSlots
            .Select(x => x.PairNumber)
            .Distinct()
            .Where(x => x >= 1 && x <= 6)
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Dispatcher"))
        {
            return Forbid();
        }

        var entry = await _db.ScheduleEntries.FindAsync(id);
        if (entry != null)
        {
            _db.ScheduleEntries.Remove(entry);
            await _db.SaveChangesAsync();
            await _audit.AddAsync("Удаление занятия", $"ID {id}");
        }

        return RedirectToPage();
    }

    public List<ScheduleEntry> GetCalendarEntries(DateTime date, int pairNumber)
    {
        return CalendarEntries
            .Where(x => x.StudyDate.Date == date.Date && x.TimeSlot?.PairNumber == pairNumber)
            .ToList();
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

    private IQueryable<ScheduleEntry> BuildBaseQuery()
    {
        var query = _db.ScheduleEntries
            .Include(x => x.Teacher)
            .Include(x => x.Group)
            .Include(x => x.Subject)
            .Include(x => x.Room)
            .Include(x => x.TimeSlot)
            .AsQueryable();

        if (GroupId.HasValue) query = query.Where(x => x.GroupId == GroupId);
        if (TeacherId.HasValue) query = query.Where(x => x.TeacherId == TeacherId);
        if (RoomId.HasValue) query = query.Where(x => x.RoomId == RoomId);

        return query;
    }

    private async Task LoadDictionariesAsync()
    {
        Groups = await _db.Groups.OrderBy(x => x.Code).ToListAsync();
        Teachers = await _db.Teachers.OrderBy(x => x.FullName).ToListAsync();
        Rooms = await _db.Rooms.OrderBy(x => x.Number).ToListAsync();
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysFromMonday);
    }
}
