namespace ScheduleCollege.Web.Services;

public class DatabaseProfile
{
    public string Provider { get; set; } = "SQLite";
    public string ConnectionString { get; set; } = "Data Source=schedulecollege.db";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool IsLocal => Provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase);

    public bool IsOnline =>
        Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
        || Provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase)
        || Provider.Equals("MariaDB", StringComparison.OrdinalIgnoreCase);
}
