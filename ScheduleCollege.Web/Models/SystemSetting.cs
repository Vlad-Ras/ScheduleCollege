using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class SystemSetting
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = "";

    [MaxLength(200)]
    public string Value { get; set; } = "";
}
