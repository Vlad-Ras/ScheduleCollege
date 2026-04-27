using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class TimeSlot
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    [Display(Name = "День недели")]
    public string DayOfWeek { get; set; } = "";

    [Display(Name = "Номер пары")]
    public int PairNumber { get; set; }

    [Required, MaxLength(5)]
    [Display(Name = "Начало")]
    public string StartTime { get; set; } = "";

    [Required, MaxLength(5)]
    [Display(Name = "Окончание")]
    public string EndTime { get; set; } = "";
}
