using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Schedule;

[Authorize(Roles = "Admin,Dispatcher")]
public class StudyDaysModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public StudyDaysModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? WeekStartDate { get; set; }

    [BindProperty]
    public DateTime WeekStartInput { get; set; }

    [BindProperty]
    public bool ApplyAllGroups { get; set; }

    [BindProperty]
    public List<int> GroupIds { get; set; } = new();

    [BindProperty]
    public List<DayInput> Days { get; set; } = new();

    public DateTime WeekStart { get; set; }
    public List<StudentGroup> Groups { get; set; } = new();
    public List<GroupStudyDay> ExistingDays { get; set; } = new();

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        WeekStart = GetWeekStart(WeekStartDate ?? DateTime.Today);
        WeekStartInput = WeekStart;
        await LoadPageDataAsync();
        BuildDefaultDays();
    }

    public async Task<IActionResult> OnPostSaveWeekAsync()
    {
        WeekStart = GetWeekStart(WeekStartInput);
        await LoadGroupsAsync();

        var targetGroups = GetTargetGroups();
        if (targetGroups.Count == 0)
        {
            ErrorMessage = "Выберите хотя бы одну группу или включите применение ко всем группам.";
            return RedirectToPage(new { WeekStartDate = WeekStart.ToString("yyyy-MM-dd") });
        }

        NormalizeDays();

        foreach (var group in targetGroups)
        {
            foreach (var day in Days)
            {
                await SaveStudyDayAsync(group.Id, day.StudyDate.Date, day.StartPairNumber, day.EndPairNumber, day.IsDayOff, day.Comment);
            }
        }

        await _db.SaveChangesAsync();
        await _audit.AddAsync("Настройка учебных дней", $"Неделя {WeekStart:dd.MM.yyyy}, групп: {targetGroups.Count}");

        Message = $"Настройки недели сохранены. Групп: {targetGroups.Count}.";
        return RedirectToPage(new { WeekStartDate = WeekStart.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostCopyPreviousWeekAsync()
    {
        WeekStart = GetWeekStart(WeekStartInput);
        await LoadGroupsAsync();

        var targetGroups = GetTargetGroups();
        if (targetGroups.Count == 0)
        {
            ErrorMessage = "Выберите хотя бы одну группу или включите применение ко всем группам.";
            return RedirectToPage(new { WeekStartDate = WeekStart.ToString("yyyy-MM-dd") });
        }

        var previousWeekStart = WeekStart.AddDays(-7);
        var copied = 0;

        foreach (var group in targetGroups)
        {
            for (var i = 0; i < 7; i++)
            {
                var sourceDate = previousWeekStart.AddDays(i).Date;
                var targetDate = WeekStart.AddDays(i).Date;

                var source = await _db.GroupStudyDays
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GroupId == group.Id && x.StudyDate.Date == sourceDate);

                if (source == null)
                {
                    continue;
                }

                await SaveStudyDayAsync(group.Id, targetDate, source.StartPairNumber, source.EndPairNumber, source.IsDayOff, source.Comment);
                copied++;
            }
        }

        await _db.SaveChangesAsync();
        await _audit.AddAsync("Копирование учебной недели", $"Неделя {previousWeekStart:dd.MM.yyyy} → {WeekStart:dd.MM.yyyy}, записей: {copied}");

        Message = copied == 0
            ? "За прошлую неделю не найдено настроек для выбранных групп."
            : $"Настройки прошлой недели скопированы. Записей: {copied}.";

        return RedirectToPage(new { WeekStartDate = WeekStart.ToString("yyyy-MM-dd") });
    }

    public static DateTime GetWeekStart(DateTime date)
    {
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-daysFromMonday);
    }

    public static string GetDayName(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Понедельник",
            DayOfWeek.Tuesday => "Вторник",
            DayOfWeek.Wednesday => "Среда",
            DayOfWeek.Thursday => "Четверг",
            DayOfWeek.Friday => "Пятница",
            DayOfWeek.Saturday => "Суббота",
            DayOfWeek.Sunday => "Воскресенье",
            _ => ""
        };
    }

    public string GetDayText(GroupStudyDay? day)
    {
        if (day == null)
        {
            return "1–6";
        }

        if (day.IsDayOff)
        {
            return "Выходной";
        }

        return $"{day.StartPairNumber}–{day.EndPairNumber}";
    }

    public GroupStudyDay? FindDay(int groupId, DateTime date)
    {
        return ExistingDays.FirstOrDefault(x => x.GroupId == groupId && x.StudyDate.Date == date.Date);
    }

    private async Task LoadPageDataAsync()
    {
        await LoadGroupsAsync();

        var end = WeekStart.AddDays(7);
        ExistingDays = await _db.GroupStudyDays
            .Include(x => x.Group)
            .Where(x => x.StudyDate >= WeekStart && x.StudyDate < end)
            .OrderBy(x => x.Group!.Code)
            .ThenBy(x => x.StudyDate)
            .ToListAsync();
    }

    private async Task LoadGroupsAsync()
    {
        Groups = await _db.Groups.OrderBy(x => x.Code).ToListAsync();
    }

    private List<StudentGroup> GetTargetGroups()
    {
        if (ApplyAllGroups)
        {
            return Groups.ToList();
        }

        return Groups.Where(x => GroupIds.Contains(x.Id)).ToList();
    }

    private void BuildDefaultDays()
    {
        Days = new List<DayInput>();

        for (var i = 0; i < 7; i++)
        {
            var date = WeekStart.AddDays(i);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            Days.Add(new DayInput
            {
                StudyDate = date,
                StartPairNumber = 1,
                EndPairNumber = 6,
                IsDayOff = isWeekend,
                Comment = isWeekend ? "Выходной" : ""
            });
        }
    }

    private void NormalizeDays()
    {
        if (Days.Count == 0)
        {
            BuildDefaultDays();
        }

        for (var i = 0; i < Days.Count; i++)
        {
            var day = Days[i];
            day.StudyDate = day.StudyDate.Date;
            day.StartPairNumber = Math.Clamp(day.StartPairNumber, 1, 6);
            day.EndPairNumber = Math.Clamp(day.EndPairNumber, 1, 6);

            if (day.StartPairNumber > day.EndPairNumber)
            {
                var oldStart = day.StartPairNumber;
                day.StartPairNumber = day.EndPairNumber;
                day.EndPairNumber = oldStart;
            }
        }
    }

    private async Task SaveStudyDayAsync(int groupId, DateTime date, int startPair, int endPair, bool isDayOff, string comment)
    {
        var item = await _db.GroupStudyDays.FirstOrDefaultAsync(x => x.GroupId == groupId && x.StudyDate.Date == date.Date);
        if (item == null)
        {
            item = new GroupStudyDay
            {
                GroupId = groupId,
                StudyDate = date.Date
            };
            _db.GroupStudyDays.Add(item);
        }

        item.StartPairNumber = Math.Clamp(startPair, 1, 6);
        item.EndPairNumber = Math.Clamp(endPair, 1, 6);
        item.IsDayOff = isDayOff;
        item.Comment = comment ?? "";
    }

    public class DayInput
    {
        public DateTime StudyDate { get; set; }
        public int StartPairNumber { get; set; } = 1;
        public int EndPairNumber { get; set; } = 6;
        public bool IsDayOff { get; set; }
        public string Comment { get; set; } = "";
    }
}
