using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;

namespace ScheduleCollege.Web.Services;

public class DatabaseTransferService
{
    private readonly AppDbContextFactory _factory;
    private readonly IWebHostEnvironment _environment;

    public DatabaseTransferService(AppDbContextFactory factory, IWebHostEnvironment environment)
    {
        _factory = factory;
        _environment = environment;
    }

    public async Task<string> CreateJsonBackupAsync(AppDbContext source)
    {
        var folder = AppPaths.BackupsDirectory;

        var backup = new DatabaseBackup
        {
            Users = await source.Users.AsNoTracking().ToListAsync(),
            Teachers = await source.Teachers.AsNoTracking().ToListAsync(),
            Groups = await source.Groups.AsNoTracking().ToListAsync(),
            Subjects = await source.Subjects.AsNoTracking().ToListAsync(),
            Rooms = await source.Rooms.AsNoTracking().ToListAsync(),
            TimeSlots = await source.TimeSlots.AsNoTracking().ToListAsync(),
            ScheduleEntries = await source.ScheduleEntries.AsNoTracking().ToListAsync(),
            AuditLogs = await source.AuditLogs.AsNoTracking().ToListAsync(),
            SystemSettings = await source.SystemSettings.AsNoTracking().ToListAsync(),
            GroupStudyDays = await source.GroupStudyDays.AsNoTracking().ToListAsync()
        };

        var fileName = $"schedulecollege_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var fullPath = Path.Combine(folder, fileName);

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fullPath, json);

        return fullPath;
    }

    public async Task<TransferResult> TransferAsync(AppDbContext source, DatabaseProfile targetProfile, bool clearTarget)
    {
        var result = new TransferResult();

        result.BackupPath = await CreateJsonBackupAsync(source);

        await using var target = _factory.Create(targetProfile);
        await target.Database.EnsureCreatedAsync();
        SchemaUpdater.Update(target);

        if (clearTarget)
        {
            target.ScheduleEntries.RemoveRange(target.ScheduleEntries);
            target.GroupStudyDays.RemoveRange(target.GroupStudyDays);
            target.AuditLogs.RemoveRange(target.AuditLogs);
            target.Users.RemoveRange(target.Users);
            target.Teachers.RemoveRange(target.Teachers);
            target.Groups.RemoveRange(target.Groups);
            target.Subjects.RemoveRange(target.Subjects);
            target.Rooms.RemoveRange(target.Rooms);
            target.TimeSlots.RemoveRange(target.TimeSlots);
            target.SystemSettings.RemoveRange(target.SystemSettings);
            await target.SaveChangesAsync();
        }

        var users = await source.Users.AsNoTracking().ToListAsync();
        var teachers = await source.Teachers.AsNoTracking().ToListAsync();
        var groups = await source.Groups.AsNoTracking().ToListAsync();
        var subjects = await source.Subjects.AsNoTracking().ToListAsync();
        var rooms = await source.Rooms.AsNoTracking().ToListAsync();
        var timeSlots = await source.TimeSlots.AsNoTracking().ToListAsync();
        var entries = await source.ScheduleEntries.AsNoTracking().ToListAsync();
        var logs = await source.AuditLogs.AsNoTracking().ToListAsync();
        var settings = await source.SystemSettings.AsNoTracking().ToListAsync();
        var groupStudyDays = await source.GroupStudyDays.AsNoTracking().ToListAsync();

        target.Users.AddRange(users.Select(x => new User { Id = x.Id, Login = x.Login, PasswordHash = x.PasswordHash, FullName = x.FullName, Role = x.Role, GroupCode = x.GroupCode, IsActive = x.IsActive }));
        target.Teachers.AddRange(teachers.Select(x => new Teacher { Id = x.Id, FullName = x.FullName, Department = x.Department, UnavailableDays = x.UnavailableDays }));
        target.Groups.AddRange(groups.Select(x => new StudentGroup { Id = x.Id, Code = x.Code, Course = x.Course, Specialty = x.Specialty }));
        target.Subjects.AddRange(subjects.Select(x => new Subject { Id = x.Id, Title = x.Title, Hours = x.Hours }));
        target.Rooms.AddRange(rooms.Select(x => new Room { Id = x.Id, Number = x.Number, Capacity = x.Capacity, Equipment = x.Equipment, Format = x.Format, OnlineLink = x.OnlineLink }));
        target.TimeSlots.AddRange(timeSlots.Select(x => new TimeSlot { Id = x.Id, DayOfWeek = x.DayOfWeek, PairNumber = x.PairNumber, StartTime = x.StartTime, EndTime = x.EndTime }));
        target.SystemSettings.AddRange(settings.Select(x => new SystemSetting { Id = x.Id, Key = x.Key, Value = x.Value }));
        target.GroupStudyDays.AddRange(groupStudyDays.Select(x => new GroupStudyDay { Id = x.Id, StudyDate = x.StudyDate, GroupId = x.GroupId, StartPairNumber = x.StartPairNumber, EndPairNumber = x.EndPairNumber, IsDayOff = x.IsDayOff, Comment = x.Comment }));
        await target.SaveChangesAsync();

        target.ScheduleEntries.AddRange(entries.Select(x => new ScheduleEntry
        {
            Id = x.Id,
            StudyDate = x.StudyDate,
            TeacherId = x.TeacherId,
            GroupId = x.GroupId,
            SubjectId = x.SubjectId,
            RoomId = x.RoomId,
            TimeSlotId = x.TimeSlotId,
            Status = x.Status,
            Note = x.Note
        }));
        await target.SaveChangesAsync();

        target.AuditLogs.AddRange(logs.Select(x => new AuditLog
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            UserLogin = x.UserLogin,
            Action = x.Action,
            Details = x.Details
        }));
        await target.SaveChangesAsync();

        result.Users = users.Count;
        result.Teachers = teachers.Count;
        result.Groups = groups.Count;
        result.Subjects = subjects.Count;
        result.Rooms = rooms.Count;
        result.TimeSlots = timeSlots.Count;
        result.ScheduleEntries = entries.Count;
        result.AuditLogs = logs.Count;
        result.SystemSettings = settings.Count;
        result.GroupStudyDays = groupStudyDays.Count;
        result.Success = true;

        return result;
    }
}

public class TransferResult
{
    public bool Success { get; set; }
    public string BackupPath { get; set; } = "";

    public int Users { get; set; }
    public int Teachers { get; set; }
    public int Groups { get; set; }
    public int Subjects { get; set; }
    public int Rooms { get; set; }
    public int TimeSlots { get; set; }
    public int ScheduleEntries { get; set; }
    public int AuditLogs { get; set; }
    public int SystemSettings { get; set; }
    public int GroupStudyDays { get; set; }

    public int Total => Users + Teachers + Groups + Subjects + Rooms + TimeSlots + ScheduleEntries + AuditLogs + SystemSettings + GroupStudyDays;
}

public class DatabaseBackup
{
    public List<User> Users { get; set; } = new();
    public List<Teacher> Teachers { get; set; } = new();
    public List<StudentGroup> Groups { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<TimeSlot> TimeSlots { get; set; } = new();
    public List<ScheduleEntry> ScheduleEntries { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();
    public List<SystemSetting> SystemSettings { get; set; } = new();
    public List<GroupStudyDay> GroupStudyDays { get; set; } = new();
}
