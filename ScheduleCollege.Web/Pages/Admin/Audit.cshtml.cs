using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;

namespace ScheduleCollege.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AuditModel : PageModel
{
    private readonly AppDbContext _db;

    public AuditModel(AppDbContext db)
    {
        _db = db;
    }

    public List<AuditLog> Logs { get; set; } = new();

    public async Task OnGetAsync()
    {
        Logs = await _db.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync();
    }
}
