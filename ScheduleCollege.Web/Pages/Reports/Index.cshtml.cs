using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ReportExportService _reportExportService;

    public IndexModel(AppDbContext db, ReportExportService reportExportService)
    {
        _db = db;
        _reportExportService = reportExportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? GroupId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    public List<StudentGroup> Groups { get; set; } = new();
    public List<StudentGroup> ReportGroups { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<TimeSlot> TimeSlots { get; set; } = new();
    public List<DateTime> ReportDates { get; set; } = new();
    public List<ScheduleEntry> Entries { get; set; } = new();

    public int LessonCount { get; set; }
    public int ExamCount { get; set; }
    public int CreditCount { get; set; }

    public string SelectedGroupText => Groups.FirstOrDefault(x => x.Id == GroupId)?.Code ?? "Все группы";
    public string SelectedRoomText => Rooms.FirstOrDefault(x => x.Id == RoomId)?.Number ?? "Все аудитории";

    public async Task OnGetAsync()
    {
        await LoadPageDataAsync();
    }

    public async Task<IActionResult> OnGetCsvAsync()
    {
        await LoadPageDataAsync();

        var csv = new StringBuilder();

        var headers = new List<string> { "День", "Время", "Пара" };
        foreach (var group in ReportGroups)
        {
            headers.Add(group.Code + " предмет");
            headers.Add(group.Code + " аудитория");
        }

        csv.AppendLine(string.Join(';', headers.Select(Csv)));

        foreach (var date in ReportDates)
        {
            if (IsWeekend(date))
            {
                var weekendRow = new List<string> { FormatDay(date), "", "ВЫХОДНОЙ" };
                for (int i = 0; i < ReportGroups.Count * 2; i++)
                {
                    weekendRow.Add("");
                }

                csv.AppendLine(string.Join(';', weekendRow.Select(Csv)));
                continue;
            }

            foreach (var slot in TimeSlots)
            {
                var row = new List<string>
                {
                    FormatDay(date),
                    FormatTime(slot),
                    FormatPair(slot.PairNumber)
                };

                foreach (var group in ReportGroups)
                {
                    var entry = FindEntry(date, slot.PairNumber, group.Id);
                    row.Add(entry?.Subject?.Title ?? "");
                    row.Add(GetRoomText(entry));
                }

                csv.AppendLine(string.Join(';', row.Select(Csv)));
            }
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var fileName = $"schedulecollege_grid_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadPageDataAsync();

        var bytes = _reportExportService.BuildPdf(
            Entries,
            ReportGroups,
            TimeSlots,
            FromDate!.Value,
            ToDate!.Value,
            SelectedGroupText,
            SelectedRoomText);

        var fileName = $"schedulecollege_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
        return File(bytes, "application/pdf", fileName);
    }

    public async Task<IActionResult> OnGetPngAsync()
    {
        await LoadPageDataAsync();

        var bytes = _reportExportService.BuildPng(
            Entries,
            ReportGroups,
            TimeSlots,
            FromDate!.Value,
            ToDate!.Value,
            SelectedGroupText,
            SelectedRoomText);

        var fileName = $"schedulecollege_{DateTime.Now:yyyyMMdd_HHmm}.png";
        return File(bytes, "image/png", fileName);
    }

    private async Task LoadPageDataAsync()
    {
        SetDefaultDates();

        Groups = await _db.Groups.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        Rooms = await _db.Rooms.AsNoTracking().OrderBy(x => x.Number).ToListAsync();
        var slots = await _db.TimeSlots.AsNoTracking().OrderBy(x => x.PairNumber).ThenBy(x => x.Id).ToListAsync();
        TimeSlots = slots
            .GroupBy(x => x.PairNumber)
            .Where(x => x.Key >= 1 && x.Key <= 6)
            .OrderBy(x => x.Key)
            .Select(x => x.First())
            .ToList();

        ReportGroups = GroupId.HasValue
            ? Groups.Where(x => x.Id == GroupId.Value).ToList()
            : Groups.ToList();

        ReportDates = BuildDateList(FromDate!.Value, ToDate!.Value);

        Entries = await BuildScheduleQuery()
            .OrderBy(x => x.StudyDate)
            .ThenBy(x => x.TimeSlot!.PairNumber)
            .ThenBy(x => x.Group!.Code)
            .ToListAsync();

        LessonCount = Entries.Count(x => !IsExam(x) && !IsCredit(x));
        ExamCount = Entries.Count(IsExam);
        CreditCount = Entries.Count(IsCredit);
    }

    public ScheduleEntry? FindEntry(DateTime date, int pairNumber, int groupId)
    {
        return Entries.FirstOrDefault(x =>
            x.StudyDate.Date == date.Date
            && x.TimeSlot?.PairNumber == pairNumber
            && x.GroupId == groupId);
    }

    public static bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    public static string FormatDay(DateTime date)
    {
        return GetRussianDayName(date.DayOfWeek) + " " + date.ToString("dd.MM");
    }

    public static string FormatTime(TimeSlot slot)
    {
        return slot.StartTime + "\n" + slot.EndTime;
    }

    public static string FormatPair(int pairNumber)
    {
        return pairNumber + " пара";
    }

    public static string GetRoomText(ScheduleEntry? entry)
    {
        if (entry?.Room == null)
        {
            return "";
        }

        if (entry.Room.Format == "Онлайн")
        {
            return "онлайн";
        }

        return entry.Room.Number;
    }

    private static bool IsExam(ScheduleEntry entry)
    {
        return (entry.Subject?.Title ?? "").Contains("экзамен", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCredit(ScheduleEntry entry)
    {
        var title = entry.Subject?.Title ?? "";
        return title.Contains("зачёт", StringComparison.OrdinalIgnoreCase)
            || title.Contains("зачет", StringComparison.OrdinalIgnoreCase);
    }

    private IQueryable<ScheduleEntry> BuildScheduleQuery()
    {
        var from = FromDate!.Value.Date;
        var toExclusive = ToDate!.Value.Date.AddDays(1);

        var query = _db.ScheduleEntries
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Group)
            .Include(x => x.Subject)
            .Include(x => x.Room)
            .Include(x => x.TimeSlot)
            .Where(x => x.StudyDate >= from && x.StudyDate < toExclusive);

        if (GroupId.HasValue)
        {
            query = query.Where(x => x.GroupId == GroupId.Value);
        }

        if (RoomId.HasValue)
        {
            query = query.Where(x => x.RoomId == RoomId.Value);
        }

        return query;
    }

    private void SetDefaultDates()
    {
        if (FromDate.HasValue || ToDate.HasValue)
        {
            FromDate ??= DateTime.Today;
            ToDate ??= FromDate.Value.AddDays(6);
            return;
        }

        var today = DateTime.Today;
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        FromDate = today.AddDays(-daysFromMonday);
        ToDate = FromDate.Value.AddDays(6);
    }

    private static List<DateTime> BuildDateList(DateTime fromDate, DateTime toDate)
    {
        var dates = new List<DateTime>();
        var current = fromDate.Date;
        var last = toDate.Date;

        while (current <= last)
        {
            dates.Add(current);
            current = current.AddDays(1);
        }

        return dates;
    }

    private static string GetRussianDayName(DayOfWeek day)
    {
        return day switch
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

    private static string Csv(string? value)
    {
        value ??= "";
        value = value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "\"\"");
        return $"\"{value}\"";
    }
}
