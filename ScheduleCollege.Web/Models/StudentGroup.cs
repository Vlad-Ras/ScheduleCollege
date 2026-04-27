using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class StudentGroup
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "Код группы")]
    public string Code { get; set; } = "";

    [Display(Name = "Курс")]
    public int Course { get; set; } = 1;

    [MaxLength(100)]
    [Display(Name = "Специальность")]
    public string Specialty { get; set; } = "";
}
