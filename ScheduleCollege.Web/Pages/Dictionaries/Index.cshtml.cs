using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Dictionaries;

[Authorize(Roles = "Admin,Dispatcher")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public IndexModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public List<global::ScheduleCollege.Web.Models.Teacher> Teachers { get; set; } = new();
    public List<StudentGroup> Groups { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<TimeSlot> TimeSlots { get; set; } = new();
    public List<TimeSlot> PairTimeSlots { get; set; } = new();

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        Teachers = await _db.Teachers.OrderBy(x => x.FullName).ToListAsync();
        Groups = await _db.Groups.OrderBy(x => x.Code).ToListAsync();
        Subjects = await _db.Subjects.OrderBy(x => x.Title).ToListAsync();
        Rooms = await _db.Rooms.OrderBy(x => x.Number).ToListAsync();
        TimeSlots = await _db.TimeSlots.OrderBy(x => x.PairNumber).ThenBy(x => x.Id).ToListAsync();
        PairTimeSlots = TimeSlots
            .GroupBy(x => x.PairNumber)
            .OrderBy(x => x.Key)
            .Select(x => x.First())
            .Where(x => x.PairNumber >= 1 && x.PairNumber <= 6)
            .ToList();
    }

    public async Task<IActionResult> OnPostAddTeacherAsync(string fullName, string department, string unavailableDays)
    {
        _db.Teachers.Add(new global::ScheduleCollege.Web.Models.Teacher { FullName = fullName, Department = department ?? "", UnavailableDays = unavailableDays ?? "" });
        await _db.SaveChangesAsync();
        await _audit.AddAsync("Добавление преподавателя", fullName);
        Message = "Преподаватель добавлен.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddGroupAsync(string code, int course, string specialty)
    {
        _db.Groups.Add(new StudentGroup { Code = code, Course = course, Specialty = specialty ?? "" });
        await _db.SaveChangesAsync();
        await _audit.AddAsync("Добавление группы", code);
        Message = "Группа добавлена.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddSubjectAsync(string title, int hours)
    {
        _db.Subjects.Add(new Subject { Title = title, Hours = hours });
        await _db.SaveChangesAsync();
        await _audit.AddAsync("Добавление дисциплины", title);
        Message = "Дисциплина добавлена.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddRoomAsync(string number, int capacity, string equipment, string format, string onlineLink)
    {
        _db.Rooms.Add(new Room
        {
            Number = number,
            Capacity = capacity,
            Equipment = equipment ?? "",
            Format = string.IsNullOrWhiteSpace(format) ? "Очная" : format,
            OnlineLink = onlineLink ?? ""
        });
        await _db.SaveChangesAsync();
        await _audit.AddAsync("Добавление аудитории", number);
        Message = "Аудитория добавлена.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddTimeSlotAsync(int pairNumber, string startTime, string endTime)
    {
        if (pairNumber < 1 || pairNumber > 6)
        {
            ErrorMessage = "Номер пары должен быть от 1 до 6.";
            return RedirectToPage();
        }

        await SavePairTimeAsync(pairNumber, startTime, endTime);
        await _audit.AddAsync("Изменение времени пары", $"{pairNumber} пара: {startTime}-{endTime}");
        Message = "Время пары сохранено.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSavePairTimesAsync(int[] pairNumber, string[] startTime, string[] endTime)
    {
        for (int i = 0; i < pairNumber.Length; i++)
        {
            if (i >= startTime.Length || i >= endTime.Length)
            {
                continue;
            }

            if (pairNumber[i] < 1 || pairNumber[i] > 6)
            {
                continue;
            }

            await SavePairTimeAsync(pairNumber[i], startTime[i], endTime[i]);
        }

        await _audit.AddAsync("Изменение времени пар", "Обновлено время пар 1-6");
        Message = "Время пар обновлено.";
        return RedirectToPage();
    }

    private async Task SavePairTimeAsync(int pairNumber, string startTime, string endTime)
    {
        startTime = (startTime ?? "").Trim();
        endTime = (endTime ?? "").Trim();

        var slots = await _db.TimeSlots.Where(x => x.PairNumber == pairNumber).ToListAsync();

        if (slots.Count == 0)
        {
            _db.TimeSlots.Add(new TimeSlot
            {
                DayOfWeek = "Каждый день",
                PairNumber = pairNumber,
                StartTime = startTime,
                EndTime = endTime
            });
        }
        else
        {
            foreach (var slot in slots)
            {
                slot.DayOfWeek = "Каждый день";
                slot.StartTime = startTime;
                slot.EndTime = endTime;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IActionResult> OnPostDeleteTeacherAsync(int id) => await DeleteAsync(_db.Teachers, id, "преподаватель");
    public async Task<IActionResult> OnPostDeleteGroupAsync(int id) => await DeleteAsync(_db.Groups, id, "группа");
    public async Task<IActionResult> OnPostDeleteSubjectAsync(int id) => await DeleteAsync(_db.Subjects, id, "дисциплина");
    public async Task<IActionResult> OnPostDeleteRoomAsync(int id) => await DeleteAsync(_db.Rooms, id, "аудитория");
    public async Task<IActionResult> OnPostDeleteTimeSlotAsync(int id) => await DeleteAsync(_db.TimeSlots, id, "пара");

    private async Task<IActionResult> DeleteAsync<TEntity>(DbSet<TEntity> set, int id, string name) where TEntity : class
    {
        var item = await set.FindAsync(id);
        if (item == null)
        {
            ErrorMessage = "Запись не найдена.";
            return RedirectToPage();
        }

        try
        {
            set.Remove(item);
            await _db.SaveChangesAsync();
            await _audit.AddAsync("Удаление справочника", name);
            Message = "Запись удалена.";
        }
        catch
        {
            ErrorMessage = "Нельзя удалить запись, если она используется в расписании.";
        }

        return RedirectToPage();
    }
}
