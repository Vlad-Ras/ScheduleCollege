using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class AuditLog
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(50)]
    public string UserLogin { get; set; } = "";

    [MaxLength(100)]
    public string Action { get; set; } = "";

    [MaxLength(500)]
    public string Details { get; set; } = "";
}
