using ScheduleCollege.Web.Models;
using ScheduleCollege.Web.Services;

namespace ScheduleCollege.Web.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        EnsureDefaultSettings(db);

        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { Login = "admin", PasswordHash = PasswordService.CreateHash("admin123"), FullName = "Администратор", Role = "Admin" },
                new User { Login = "dispatcher", PasswordHash = PasswordService.CreateHash("dispatcher123"), FullName = "Диспетчер расписания", Role = "Dispatcher" },
                new User { Login = "teacher", PasswordHash = PasswordService.CreateHash("teacher123"), FullName = "Иванов Иван Иванович", Role = "Teacher" },
                new User { Login = "student", PasswordHash = PasswordService.CreateHash("student123"), FullName = "Студент ИС-31", Role = "Student", GroupCode = "ИС-31" }
            );
            db.SaveChanges();
        }

        var studentUser = db.Users.FirstOrDefault(x => x.Login == "student");
        if (studentUser != null && string.IsNullOrWhiteSpace(studentUser.GroupCode))
        {
            studentUser.GroupCode = "ИС-31";
            db.SaveChanges();
        }

        if (!db.Teachers.Any())
        {
            db.Teachers.AddRange(
                new Teacher { FullName = "Иванов Иван Иванович", Department = "Информационные технологии", UnavailableDays = "Пятница" },
                new Teacher { FullName = "Петрова Анна Сергеевна", Department = "Математика", UnavailableDays = "Среда" },
                new Teacher { FullName = "Сидоров Павел Олегович", Department = "Общеобразовательные дисциплины", UnavailableDays = "" }
            );
            db.SaveChanges();
        }

        if (!db.Groups.Any())
        {
            db.Groups.AddRange(
                new StudentGroup { Code = "ИС-31", Course = 3, Specialty = "09.02.07 Информационные системы и программирование" },
                new StudentGroup { Code = "ИС-21", Course = 2, Specialty = "09.02.07 Информационные системы и программирование" },
                new StudentGroup { Code = "ПК-11", Course = 1, Specialty = "09.02.07 Информационные системы и программирование" }
            );
            db.SaveChanges();
        }

        if (!db.Subjects.Any())
        {
            db.Subjects.AddRange(
                new Subject { Title = "Разработка программных модулей", Hours = 72 },
                new Subject { Title = "Базы данных", Hours = 64 },
                new Subject { Title = "Математика", Hours = 60 },
                new Subject { Title = "Математика (зачёт)", Hours = 60 },
                new Subject { Title = "Информационные системы", Hours = 80 }
            );
            db.SaveChanges();
        }

        if (!db.Rooms.Any())
        {
            db.Rooms.AddRange(
                new Room { Number = "204", Capacity = 30, Equipment = "ПК, проектор", Format = "Очная" },
                new Room { Number = "305-А", Capacity = 25, Equipment = "ПК", Format = "Очная" },
                new Room { Number = "Актовый зал", Capacity = 80, Equipment = "Проектор", Format = "Очная" },
                new Room { Number = "Онлайн", Capacity = 0, Equipment = "Видеоконференция", Format = "Онлайн", OnlineLink = "https://meet.example.edu/schedule" }
            );
            db.SaveChanges();
        }

        EnsureDefaultTimeSlots(db);
        EnsureDefaultGroupStudyDays(db);

        if (!db.ScheduleEntries.Any())
        {
            var today = DateTime.Today;
            var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
            var monday = today.AddDays(-daysFromMonday);

            db.ScheduleEntries.AddRange(
                new ScheduleEntry
                {
                    StudyDate = monday,
                    TeacherId = db.Teachers.First().Id,
                    GroupId = db.Groups.First(g => g.Code == "ИС-31").Id,
                    SubjectId = db.Subjects.First(s => s.Title == "Базы данных").Id,
                    RoomId = db.Rooms.First(r => r.Number == "204").Id,
                    TimeSlotId = db.TimeSlots.OrderBy(t => t.Id).First(t => t.PairNumber == 1).Id,
                    Status = "Опубликовано"
                },
                new ScheduleEntry
                {
                    StudyDate = monday.AddDays(1),
                    TeacherId = db.Teachers.First(t => t.FullName.StartsWith("Петрова")).Id,
                    GroupId = db.Groups.First(g => g.Code == "ИС-21").Id,
                    SubjectId = db.Subjects.First(s => s.Title == "Математика (зачёт)").Id,
                    RoomId = db.Rooms.First(r => r.Number == "Актовый зал").Id,
                    TimeSlotId = db.TimeSlots.OrderBy(t => t.Id).First(t => t.PairNumber == 2).Id,
                    Status = "Опубликовано"
                }
            );
            db.SaveChanges();
        }
    }


    private static void EnsureDefaultGroupStudyDays(AppDbContext db)
    {
        if (db.GroupStudyDays.Any() || !db.Groups.Any())
        {
            return;
        }

        var today = DateTime.Today;
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysFromMonday);

        var groups = db.Groups.OrderBy(x => x.Code).ToList();

        foreach (var group in groups)
        {
            for (var day = 0; day < 7; day++)
            {
                var date = monday.AddDays(day);
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                db.GroupStudyDays.Add(new GroupStudyDay
                {
                    GroupId = group.Id,
                    StudyDate = date,
                    StartPairNumber = isWeekend ? 1 : 1,
                    EndPairNumber = isWeekend ? 1 : 6,
                    IsDayOff = isWeekend,
                    Comment = isWeekend ? "Выходной" : ""
                });
            }
        }

        db.SaveChanges();
    }

    private static void EnsureDefaultSettings(AppDbContext db)
    {
        if (!db.SystemSettings.Any(x => x.Key == "StudentCanViewAllGroups"))
        {
            db.SystemSettings.Add(new SystemSetting { Key = "StudentCanViewAllGroups", Value = "false" });
            db.SaveChanges();
        }
    }

    private static void EnsureDefaultTimeSlots(AppDbContext db)
    {
        var defaults = new[]
        {
            new { Pair = 1, Start = "09:00", End = "10:30" },
            new { Pair = 2, Start = "10:40", End = "12:10" },
            new { Pair = 3, Start = "12:40", End = "14:10" },
            new { Pair = 4, Start = "14:20", End = "15:50" },
            new { Pair = 5, Start = "16:00", End = "17:30" },
            new { Pair = 6, Start = "17:40", End = "19:10" }
        };

        foreach (var item in defaults)
        {
            var existing = db.TimeSlots.Where(x => x.PairNumber == item.Pair).ToList();
            if (existing.Count == 0)
            {
                db.TimeSlots.Add(new TimeSlot
                {
                    DayOfWeek = "Каждый день",
                    PairNumber = item.Pair,
                    StartTime = item.Start,
                    EndTime = item.End
                });
                continue;
            }

            foreach (var slot in existing)
            {
                slot.DayOfWeek = "Каждый день";
                slot.StartTime = item.Start;
                slot.EndTime = item.End;
            }
        }

        db.SaveChanges();
    }
}
