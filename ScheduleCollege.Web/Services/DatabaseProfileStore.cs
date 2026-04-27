using System.Text.Json;

namespace ScheduleCollege.Web.Services;

public class DatabaseProfileStore
{
    private readonly string _filePath;
    private readonly string _legacyFilePath;
    private readonly object _lock = new();

    public DatabaseProfileStore(IWebHostEnvironment environment)
    {
        _filePath = Path.Combine(AppPaths.DataDirectory, "database-profile.json");
        _legacyFilePath = Path.Combine(environment.ContentRootPath, "data", "database-profile.json");

        if (!File.Exists(_filePath) && File.Exists(_legacyFilePath))
        {
            File.Copy(_legacyFilePath, _filePath);
        }
    }

    public DatabaseProfile GetCurrentProfile()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                var defaultProfile = new DatabaseProfile();
                Save(defaultProfile);
                return defaultProfile;
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<DatabaseProfile>(json) ?? new DatabaseProfile();
        }
    }

    public void Save(DatabaseProfile profile)
    {
        lock (_lock)
        {
            profile.UpdatedAt = DateTime.Now;
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
