using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DatabaseModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly DatabaseProfileStore _profileStore;
    private readonly AppDbContextFactory _factory;
    private readonly DatabaseTransferService _transferService;
    private readonly AuditService _audit;

    public DatabaseModel(
        AppDbContext db,
        DatabaseProfileStore profileStore,
        AppDbContextFactory factory,
        DatabaseTransferService transferService,
        AuditService audit)
    {
        _db = db;
        _profileStore = profileStore;
        _factory = factory;
        _transferService = transferService;
        _audit = audit;
    }

    public string CurrentProvider { get; set; } = "";
    public string CurrentConnectionString { get; set; } = "";

    public int UsersCount { get; set; }
    public int TeachersCount { get; set; }
    public int GroupsCount { get; set; }
    public int SubjectsCount { get; set; }
    public int RoomsCount { get; set; }
    public int ScheduleCount { get; set; }

    [BindProperty]
    public string TargetProvider { get; set; } = "SQLite";

    [BindProperty]
    public string TargetConnectionString { get; set; } = "";

    [BindProperty]
    public string SqliteFile { get; set; } = "schedulecollege_copy.db";

    [BindProperty]
    public string Server { get; set; } = "localhost";

    [BindProperty]
    public int Port { get; set; } = 5432;

    [BindProperty]
    public string DatabaseName { get; set; } = "schedulecollege";

    [BindProperty]
    public string Username { get; set; } = "user";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool ClearTarget { get; set; } = true;

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostBackupAsync()
    {
        try
        {
            var path = await _transferService.CreateJsonBackupAsync(_db);
            await _audit.AddAsync("Резервная копия БД", path);
            Message = $"Резервная копия создана: {path}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка резервного копирования: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestAsync()
    {
        try
        {
            var profile = BuildTargetProfile();
            await using var target = _factory.Create(profile);
            await target.Database.EnsureCreatedAsync();
            SchemaUpdater.Update(target);
            var canConnect = await target.Database.CanConnectAsync();

            Message = canConnect
                ? "Подключение успешно проверено."
                : "Подключение не удалось проверить.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка подключения: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTransferAsync()
    {
        try
        {
            var profile = BuildTargetProfile();
            var current = _profileStore.GetCurrentProfile();

            if (string.Equals(current.Provider, profile.Provider, StringComparison.OrdinalIgnoreCase)
                && string.Equals(current.ConnectionString, profile.ConnectionString, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Целевая база совпадает с текущей. Укажите другую базу, чтобы не потерять данные.";
                return RedirectToPage();
            }

            var result = await _transferService.TransferAsync(_db, profile, ClearTarget);

            _profileStore.Save(profile);

            await _audit.AddAsync("Переключение базы данных", $"{profile.Provider}, перенесено записей: {result.Total}");
            Message = $"Готово. Перенесено записей: {result.Total}. Backup: {result.BackupPath}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Перенос не выполнен: {ex.Message}";
        }

        return RedirectToPage();
    }

    private DatabaseProfile BuildTargetProfile()
    {
        if (string.IsNullOrWhiteSpace(TargetProvider))
        {
            TargetProvider = "SQLite";
        }

        var connectionString = BuildConnectionString();

        return new DatabaseProfile
        {
            Provider = TargetProvider,
            ConnectionString = connectionString
        };
    }

    private string BuildConnectionString()
    {
        if (TargetProvider == "SQLite")
        {
            if (!string.IsNullOrWhiteSpace(TargetConnectionString))
            {
                return TargetConnectionString;
            }

            if (string.IsNullOrWhiteSpace(SqliteFile))
            {
                SqliteFile = "schedulecollege_copy.db";
            }

            return $"Data Source={SqliteFile}";
        }

        if (!string.IsNullOrWhiteSpace(TargetConnectionString))
        {
            return TargetConnectionString;
        }

        if (TargetProvider == "PostgreSQL")
        {
            if (Port <= 0) Port = 5432;
            return $"Host={Server};Port={Port};Database={DatabaseName};Username={Username};Password={Password}";
        }

        if (TargetProvider == "MySQL" || TargetProvider == "MariaDB")
        {
            if (Port <= 0) Port = 3306;
            return $"Server={Server};Port={Port};Database={DatabaseName};User={Username};Password={Password};";
        }

        return "";
    }

    private async Task LoadAsync()
    {
        var profile = _profileStore.GetCurrentProfile();
        CurrentProvider = profile.Provider;
        CurrentConnectionString = profile.ConnectionString;

        UsersCount = await _db.Users.CountAsync();
        TeachersCount = await _db.Teachers.CountAsync();
        GroupsCount = await _db.Groups.CountAsync();
        SubjectsCount = await _db.Subjects.CountAsync();
        RoomsCount = await _db.Rooms.CountAsync();
        ScheduleCount = await _db.ScheduleEntries.CountAsync();
    }
}
