using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public SettingsModel(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty]
    public bool StudentCanViewAllGroups { get; set; }

    [TempData] public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        StudentCanViewAllGroups = await GetBoolSettingAsync("StudentCanViewAllGroups", false);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await SetSettingAsync("StudentCanViewAllGroups", StudentCanViewAllGroups ? "true" : "false");
        await _audit.AddAsync("Изменение настроек", StudentCanViewAllGroups ? "Студент видит все группы" : "Студент видит только свою группу");
        Message = "Настройки сохранены.";
        return RedirectToPage();
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

    private async Task SetSettingAsync(string key, string value)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (setting == null)
        {
            _db.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await _db.SaveChangesAsync();
    }
}
