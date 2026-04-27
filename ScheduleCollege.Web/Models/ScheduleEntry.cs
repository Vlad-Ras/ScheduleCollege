using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class ScheduleEntry
{
    public int Id { get; set; }

    [Display(Name = "Дата")]
    public DateTime StudyDate { get; set; } = DateTime.Today;

    [Display(Name = "Преподаватель")]
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    [Display(Name = "Группа")]
    public int GroupId { get; set; }
    public StudentGroup? Group { get; set; }

    [Display(Name = "Дисциплина")]
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    [Display(Name = "Аудитория")]
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    [Display(Name = "Пара")]
    public int TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }

    [MaxLength(30)]
    [Display(Name = "Статус")]
    public string Status { get; set; } = "Опубликовано";

    [MaxLength(200)]
    [Display(Name = "Примечание")]
    public string Note { get; set; } = "";
}
