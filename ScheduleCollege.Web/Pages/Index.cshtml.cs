using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Models;

namespace ScheduleCollege.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public int TeacherCount { get; set; }
    public int GroupCount { get; set; }
    public int RoomCount { get; set; }
    public int ScheduleCount { get; set; }
    public List<ScheduleEntry> Upcoming { get; set; } = new();

    public string GetRoomText(Room? room)
    {
        if (room == null)
        {
            return "";
        }

        if (room.Format == "Онлайн" && !string.IsNullOrWhiteSpace(room.OnlineLink))
        {
            return $"{room.Number} ({room.OnlineLink})";
        }

        return room.Format == "Онлайн" ? $"{room.Number} (онлайн)" : room.Number;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.IsInRole("Student"))
        {
            return RedirectToPage("/Student/Schedule");
        }

        TeacherCount = await _db.Teachers.CountAsync();
        GroupCount = await _db.Groups.CountAsync();
        RoomCount = await _db.Rooms.CountAsync();
        ScheduleCount = await _db.ScheduleEntries.CountAsync();

        Upcoming = await _db.ScheduleEntries
            .Include(x => x.Teacher)
            .Include(x => x.Group)
            .Include(x => x.Subject)
            .Include(x => x.Room)
            .Include(x => x.TimeSlot)
            .OrderBy(x => x.StudyDate)
            .ThenBy(x => x.TimeSlot!.PairNumber)
            .Take(8)
            .ToListAsync();

        return Page();
    }
}
