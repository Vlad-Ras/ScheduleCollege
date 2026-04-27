using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Schedule;

[Authorize(Roles = "Admin,Dispatcher")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public EditModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty]
    public ScheduleEntry Entry { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? SelectedDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedTimeSlotId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedGroupId { get; set; }

    public string ErrorMessage { get; set; } = "";
    public string InfoMessage { get; set; } = "";

    public List<SelectListItem> TeacherItems { get; set; } = new();
    public List<SelectListItem> GroupItems { get; set; } = new();
    public List<SelectListItem> SubjectItems { get; set; } = new();
    public List<SelectListItem> RoomItems { get; set; } = new();
    public List<SelectListItem> TimeSlotItems { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var entry = await _db.ScheduleEntries.FindAsync(id);
        if (entry == null) return NotFound();

        Entry = entry;

        if (SelectedDate.HasValue)
        {
            Entry.StudyDate = SelectedDate.Value;
        }

        if (SelectedGroupId.HasValue)
        {
            Entry.GroupId = SelectedGroupId.Value;
        }

        if (SelectedTimeSlotId.HasValue)
        {
            Entry.TimeSlotId = SelectedTimeSlotId.Value;
        }

        await LoadListsAsync(Entry.Id, Entry.TeacherId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadListsAsync(Entry.Id, Entry.TeacherId);

        var slotError = await ValidateSlotAsync(Entry.TimeSlotId);
        if (!string.IsNullOrWhiteSpace(slotError))
        {
            ErrorMessage = slotError;
            return Page();
        }

        var groupDayError = await ValidateGroupStudyDayAsync(Entry.StudyDate, Entry.GroupId, Entry.TimeSlotId);
        if (!string.IsNullOrWhiteSpace(groupDayError))
        {
            ErrorMessage = groupDayError;
            return Page();
        }

        var teacherError = await ValidateTeacherAvailabilityAsync(Entry.Id, Entry.StudyDate, Entry.TimeSlotId, Entry.TeacherId);
        if (!string.IsNullOrWhiteSpace(teacherError))
        {
            ErrorMessage = teacherError;
            return Page();
        }

        var conflict = await FindConflictAsync(Entry.Id, Entry.StudyDate, Entry.TimeSlotId, Entry.TeacherId, Entry.GroupId, Entry.RoomId);
        if (!string.IsNullOrWhiteSpace(conflict))
        {
            ErrorMessage = conflict;
            return Page();
        }

        _db.ScheduleEntries.Update(Entry);
        await _db.SaveChangesAsync();
        await _audit.AddAsync("Изменение занятия", $"ID {Entry.Id}");

        return RedirectToPage("/Schedule/Index");
    }

    private async Task<string> FindConflictAsync(int? currentId, DateTime date, int slotId, int teacherId, int groupId, int roomId)
    {
        var pairNumber = await GetPairNumberAsync(slotId);
        if (pairNumber == 0)
        {
            return "Выбранная пара не найдена.";
        }

        var query = _db.ScheduleEntries.Where(x =>
            x.StudyDate.Date == date.Date
            && x.TimeSlot != null
            && x.TimeSlot.PairNumber == pairNumber);

        if (currentId.HasValue)
        {
            query = query.Where(x => x.Id != currentId.Value);
        }

        if (await query.AnyAsync(x => x.TeacherId == teacherId)) return "Конфликт: преподаватель уже занят в эту пару.";
        if (await query.AnyAsync(x => x.GroupId == groupId)) return "Конфликт: группа уже занята в эту пару.";
        if (await query.AnyAsync(x => x.RoomId == roomId)) return "Конфликт: аудитория уже занята в эту пару.";

        return "";
    }

    private async Task<string> ValidateSlotAsync(int slotId)
    {
        var slot = await _db.TimeSlots.FindAsync(slotId);
        if (slot == null)
        {
            return "Выбранная пара не найдена.";
        }

        if (slot.PairNumber < 1 || slot.PairNumber > 6)
        {
            return "Номер пары должен быть от 1 до 6.";
        }

        return "";
    }

    private async Task<string> ValidateGroupStudyDayAsync(DateTime date, int groupId, int slotId)
    {
        var pairNumber = await GetPairNumberAsync(slotId);
        var settings = await GetGroupDaySettingsAsync(date, groupId);

        if (settings.IsDayOff)
        {
            return "Для выбранной группы этот день отмечен как выходной.";
        }

        if (pairNumber < settings.StartPairNumber || pairNumber > settings.EndPairNumber)
        {
            return $"Для выбранной группы разрешены пары с {settings.StartPairNumber} по {settings.EndPairNumber}.";
        }

        return "";
    }

    private async Task<string> ValidateTeacherAvailabilityAsync(int? currentId, DateTime date, int slotId, int teacherId)
    {
        var teacher = await _db.Teachers.FindAsync(teacherId);
        if (teacher == null)
        {
            return "Выбранный преподаватель не найден.";
        }

        var dayName = GetRussianDayName(date);
        if (IsTeacherUnavailable(teacher, dayName))
        {
            return $"Преподаватель недоступен в день \"{dayName}\".";
        }

        var pairNumber = await GetPairNumberAsync(slotId);
        var query = _db.ScheduleEntries.Where(x =>
            x.StudyDate.Date == date.Date
            && x.TimeSlot != null
            && x.TimeSlot.PairNumber == pairNumber);

        if (currentId.HasValue)
        {
            query = query.Where(x => x.Id != currentId.Value);
        }

        if (await query.AnyAsync(x => x.TeacherId == teacherId))
        {
            return "Преподаватель уже занят в выбранную дату и пару.";
        }

        return "";
    }

    private async Task LoadListsAsync(int? currentEntryId, int? selectedTeacherId)
    {
        GroupItems = await _db.Groups.OrderBy(x => x.Code).Select(x => new SelectListItem(x.Code, x.Id.ToString())).ToListAsync();
        SubjectItems = await _db.Subjects.OrderBy(x => x.Title).Select(x => new SelectListItem(x.Title, x.Id.ToString())).ToListAsync();
        RoomItems = await _db.Rooms
            .OrderBy(x => x.Number)
            .Select(x => new SelectListItem(x.Format == "Онлайн" ? $"{x.Number} (онлайн)" : x.Number, x.Id.ToString()))
            .ToListAsync();

        if (Entry.GroupId == 0 && GroupItems.Count > 0)
        {
            Entry.GroupId = int.Parse(GroupItems[0].Value ?? "0");
        }

        var settings = await GetGroupDaySettingsAsync(Entry.StudyDate, Entry.GroupId);
        var allSlots = await _db.TimeSlots.OrderBy(x => x.PairNumber).ThenBy(x => x.Id).ToListAsync();
        var allowedSlots = allSlots
            .GroupBy(x => x.PairNumber)
            .Where(x => x.Key >= 1 && x.Key <= 6)
            .OrderBy(x => x.Key)
            .Select(x => x.First())
            .Where(x => !settings.IsDayOff && x.PairNumber >= settings.StartPairNumber && x.PairNumber <= settings.EndPairNumber)
            .ToList();

        TimeSlotItems = allowedSlots
            .Select(x => new SelectListItem($"{x.PairNumber} пара ({x.StartTime}-{x.EndTime})", x.Id.ToString()))
            .ToList();

        if (TimeSlotItems.Count == 0)
        {
            Entry.TimeSlotId = 0;
            InfoMessage = "Для выбранной группы этот день закрыт или нет доступных пар.";
        }
        else if (Entry.TimeSlotId == 0 || allowedSlots.All(x => x.Id != Entry.TimeSlotId))
        {
            Entry.TimeSlotId = allowedSlots.First().Id;
        }

        var teachers = await _db.Teachers.OrderBy(x => x.FullName).ToListAsync();
        var dayName = GetRussianDayName(Entry.StudyDate);
        var pairNumber = await GetPairNumberAsync(Entry.TimeSlotId);

        var busyTeacherIdsQuery = _db.ScheduleEntries.Where(x =>
            x.StudyDate.Date == Entry.StudyDate.Date
            && x.TimeSlot != null
            && x.TimeSlot.PairNumber == pairNumber);

        if (currentEntryId.HasValue)
        {
            busyTeacherIdsQuery = busyTeacherIdsQuery.Where(x => x.Id != currentEntryId.Value);
        }

        var busyTeacherIds = await busyTeacherIdsQuery.Select(x => x.TeacherId).ToListAsync();

        var filteredTeachers = teachers
            .Where(x => !busyTeacherIds.Contains(x.Id) || selectedTeacherId == x.Id)
            .Where(x => !IsTeacherUnavailable(x, dayName) || selectedTeacherId == x.Id)
            .ToList();

        var hiddenCount = teachers.Count - filteredTeachers.Count;
        if (hiddenCount > 0)
        {
            var text = $"Скрыто недоступных или занятых преподавателей: {hiddenCount}.";
            InfoMessage = string.IsNullOrWhiteSpace(InfoMessage) ? text : InfoMessage + " " + text;
        }

        TeacherItems = filteredTeachers
            .Select(x => new SelectListItem(BuildTeacherText(x), x.Id.ToString()))
            .ToList();
    }

    private async Task<int> GetPairNumberAsync(int slotId)
    {
        return await _db.TimeSlots
            .Where(x => x.Id == slotId)
            .Select(x => x.PairNumber)
            .FirstOrDefaultAsync();
    }

    private async Task<GroupDaySettings> GetGroupDaySettingsAsync(DateTime date, int groupId)
    {
        var item = await _db.GroupStudyDays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GroupId == groupId && x.StudyDate.Date == date.Date);

        if (item == null)
        {
            return new GroupDaySettings { StartPairNumber = 1, EndPairNumber = 6, IsDayOff = false };
        }

        return new GroupDaySettings
        {
            StartPairNumber = item.StartPairNumber,
            EndPairNumber = item.EndPairNumber,
            IsDayOff = item.IsDayOff
        };
    }

    private static string BuildTeacherText(global::ScheduleCollege.Web.Models.Teacher teacher)
    {
        if (string.IsNullOrWhiteSpace(teacher.UnavailableDays))
        {
            return teacher.FullName;
        }

        return $"{teacher.FullName} (недоступен: {teacher.UnavailableDays})";
    }

    private static bool IsTeacherUnavailable(global::ScheduleCollege.Web.Models.Teacher teacher, string dayName)
    {
        if (string.IsNullOrWhiteSpace(teacher.UnavailableDays))
        {
            return false;
        }

        return teacher.UnavailableDays
            .Split(',', ';')
            .Select(x => x.Trim())
            .Any(x => string.Equals(x, dayName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRussianDayName(DateTime date)
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

    private class GroupDaySettings
    {
        public int StartPairNumber { get; set; } = 1;
        public int EndPairNumber { get; set; } = 6;
        public bool IsDayOff { get; set; }
    }
}
