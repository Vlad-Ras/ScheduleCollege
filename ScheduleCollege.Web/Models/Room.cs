using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class Room
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Название или номер аудитории")]
    public string Number { get; set; } = "";

    [Display(Name = "Количество мест")]
    public int Capacity { get; set; }

    [MaxLength(100)]
    [Display(Name = "Оснащение")]
    public string Equipment { get; set; } = "";

    [MaxLength(20)]
    [Display(Name = "Формат")]
    public string Format { get; set; } = "Очная";

    [MaxLength(200)]
    [Display(Name = "Ссылка для онлайн-занятия")]
    public string OnlineLink { get; set; } = "";
}
