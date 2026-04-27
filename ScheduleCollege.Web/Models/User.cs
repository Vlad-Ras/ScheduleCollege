using System.ComponentModel.DataAnnotations;

namespace ScheduleCollege.Web.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Login { get; set; } = "";

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = "";

    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(30)]
    public string Role { get; set; } = "Student";

    [MaxLength(30)]
    [Display(Name = "Группа студента")]
    public string GroupCode { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
