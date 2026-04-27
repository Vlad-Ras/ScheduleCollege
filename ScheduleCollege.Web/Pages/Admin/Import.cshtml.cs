using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Admin;

[Authorize(Roles = "Admin,Dispatcher")]
public class ImportModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public ImportModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty]
    public string ImportType { get; set; } = "Rooms";

    [BindProperty]
    public IFormFile? UploadFile { get; set; }

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadFile == null || UploadFile.Length == 0)
        {
            ErrorMessage = "Выберите файл для импорта.";
            return RedirectToPage();
        }

        var extension = Path.GetExtension(UploadFile.FileName).ToLowerInvariant();
        var rows = await ReadRowsAsync(UploadFile, extension);

        if (rows.Count == 0)
        {
            ErrorMessage = "В файле нет строк для импорта.";
            return RedirectToPage();
        }

        var imported = ImportType switch
        {
            "Rooms" => await ImportRoomsAsync(rows),
            "Students" => await ImportStudentsAsync(rows),
            "TimeSlots" => await ImportTimeSlotsAsync(rows),
            _ => 0
        };

        await _audit.AddAsync("Импорт данных", $"{ImportType}: {imported} записей");
        Message = $"Импорт завершён. Обработано записей: {imported}.";
        return RedirectToPage();
    }

    private static async Task<List<string[]>> ReadRowsAsync(IFormFile file, string extension)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        if (extension == ".json")
        {
            return ReadJsonRows(text);
        }

        return text
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseTextLine)
            .Where(x => x.Length > 0)
            .ToList();
    }

    private static string[] ParseTextLine(string line)
    {
        line = line.Trim();
        if (string.IsNullOrWhiteSpace(line))
        {
            return Array.Empty<string>();
        }

        var separator = ';';
        if (line.Contains('\t')) separator = '\t';
        else if (!line.Contains(';') && line.Contains(',')) separator = ',';

        return line.Split(separator).Select(x => x.Trim().Trim('"')).ToArray();
    }

    private static List<string[]> ReadJsonRows(string text)
    {
        var rows = new List<string[]>();
        using var document = JsonDocument.Parse(text);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return rows;
        }

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var values = item.EnumerateObject()
                .Select(x => x.Value.ToString()?.Trim() ?? "")
                .ToArray();

            rows.Add(values);
        }

        return rows;
    }

    private static bool LooksLikeHeader(string[] row)
    {
        var first = row.FirstOrDefault()?.ToLowerInvariant() ?? "";
        return first.Contains("login")
            || first.Contains("логин")
            || first.Contains("number")
            || first.Contains("ауд")
            || first.Contains("pair")
            || first.Contains("пара");
    }

    private async Task<int> ImportRoomsAsync(List<string[]> rows)
    {
        var count = 0;

        foreach (var row in rows)
        {
            if (row.Length == 0 || LooksLikeHeader(row))
            {
                continue;
            }

            var number = Get(row, 0);
            if (string.IsNullOrWhiteSpace(number))
            {
                continue;
            }

            var capacity = ToInt(Get(row, 1), 0);
            var equipment = Get(row, 2);
            var format = string.IsNullOrWhiteSpace(Get(row, 3)) ? "Очная" : Get(row, 3);
            var onlineLink = Get(row, 4);

            var room = await _db.Rooms.FirstOrDefaultAsync(x => x.Number == number);
            if (room == null)
            {
                _db.Rooms.Add(new Room
                {
                    Number = number,
                    Capacity = capacity,
                    Equipment = equipment,
                    Format = format,
                    OnlineLink = onlineLink
                });
            }
            else
            {
                room.Capacity = capacity;
                room.Equipment = equipment;
                room.Format = format;
                room.OnlineLink = onlineLink;
            }

            count++;
        }

        await _db.SaveChangesAsync();
        return count;
    }

    private async Task<int> ImportStudentsAsync(List<string[]> rows)
    {
        var count = 0;

        foreach (var row in rows)
        {
            if (row.Length == 0 || LooksLikeHeader(row))
            {
                continue;
            }

            var login = Get(row, 0);
            var fullName = Get(row, 1);
            var groupCode = Get(row, 2);
            var password = string.IsNullOrWhiteSpace(Get(row, 3)) ? "student123" : Get(row, 3);

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Login == login);
            if (user == null)
            {
                _db.Users.Add(new User
                {
                    Login = login,
                    FullName = fullName,
                    GroupCode = groupCode,
                    Role = "Student",
                    PasswordHash = PasswordService.CreateHash(password),
                    IsActive = true
                });
            }
            else
            {
                user.FullName = fullName;
                user.GroupCode = groupCode;
                user.Role = "Student";
            }

            count++;
        }

        await _db.SaveChangesAsync();
        return count;
    }

    private async Task<int> ImportTimeSlotsAsync(List<string[]> rows)
    {
        var count = 0;

        foreach (var row in rows)
        {
            if (row.Length == 0 || LooksLikeHeader(row))
            {
                continue;
            }

            var pairNumber = ToInt(Get(row, 0), 0);
            var startTime = Get(row, 1);
            var endTime = Get(row, 2);

            if (pairNumber < 1 || pairNumber > 6 || string.IsNullOrWhiteSpace(startTime) || string.IsNullOrWhiteSpace(endTime))
            {
                continue;
            }

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

            count++;
        }

        await _db.SaveChangesAsync();
        return count;
    }

    private static string Get(string[] row, int index)
    {
        return index >= 0 && index < row.Length ? row[index].Trim() : "";
    }

    private static int ToInt(string value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}
