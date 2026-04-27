using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class Teacher
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "ФИО преподавателя")]
    public string FullName { get; set; } = "";

    [MaxLength(100)]
    [Display(Name = "Отделение")]
    public string Department { get; set; } = "";

    [MaxLength(200)]
    [Display(Name = "Дни, когда преподаватель недоступен")]
    public string UnavailableDays { get; set; } = "";
}
