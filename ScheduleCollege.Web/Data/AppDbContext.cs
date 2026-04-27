using Microsoft.EntityFrameworkCore;
using ScheduleCollege.Web.Models;

namespace ScheduleCollege.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<StudentGroup> Groups => Set<StudentGroup>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<GroupStudyDay> GroupStudyDays => Set<GroupStudyDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Login).IsUnique();
        modelBuilder.Entity<Teacher>().HasIndex(x => x.FullName).IsUnique();
        modelBuilder.Entity<StudentGroup>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Room>().HasIndex(x => x.Number).IsUnique();
        modelBuilder.Entity<SystemSetting>().HasIndex(x => x.Key).IsUnique();

        modelBuilder.Entity<ScheduleEntry>()
            .HasIndex(x => new { x.StudyDate, x.TimeSlotId, x.RoomId })
            .IsUnique();

        modelBuilder.Entity<ScheduleEntry>()
            .HasIndex(x => new { x.StudyDate, x.TimeSlotId, x.TeacherId })
            .IsUnique();

        modelBuilder.Entity<ScheduleEntry>()
            .HasIndex(x => new { x.StudyDate, x.TimeSlotId, x.GroupId })
            .IsUnique();


        modelBuilder.Entity<GroupStudyDay>()
            .HasIndex(x => new { x.StudyDate, x.GroupId })
            .IsUnique();

        modelBuilder.Entity<GroupStudyDay>()
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScheduleEntry>()
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduleEntry>()
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduleEntry>()
            .HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduleEntry>()
            .HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScheduleEntry>()
            .HasOne(x => x.TimeSlot)
            .WithMany()
            .HasForeignKey(x => x.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
