using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class Subject
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Название дисциплины")]
    public string Title { get; set; } = "";

    [Display(Name = "Часы")]
    public int Hours { get; set; }
}
