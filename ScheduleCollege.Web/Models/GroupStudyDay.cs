using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class GroupStudyDay
{
    public int Id { get; set; }

    [Display(Name = "Дата")]
    public DateTime StudyDate { get; set; } = DateTime.Today;

    [Display(Name = "Группа")]
    public int GroupId { get; set; }
    public StudentGroup? Group { get; set; }

    [Display(Name = "Начальная пара")]
    public int StartPairNumber { get; set; } = 1;

    [Display(Name = "Последняя пара")]
    public int EndPairNumber { get; set; } = 6;

    [Display(Name = "Выходной")]
    public bool IsDayOff { get; set; }

    [MaxLength(200)]
    [Display(Name = "Комментарий")]
    public string Comment { get; set; } = "";
}
