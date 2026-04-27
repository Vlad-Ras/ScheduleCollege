using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;

namespace ScheduleCollege.Web.Services;

public class AuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task AddAsync(string action, string details)
    {
        var login = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";

        _db.AuditLogs.Add(new AuditLog
        {
            CreatedAt = DateTime.Now,
            UserLogin = login,
            Action = action,
            Details = details
        });

        await _db.SaveChangesAsync();
    }
}
