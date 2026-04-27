using Microsoft.Data.Sqlite;

namespace ScheduleCollege.Web.Services;

public static class AppPaths
{
    public static string AppDataRoot
    {
        get
        {
            var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(baseFolder))
            {
                baseFolder = AppContext.BaseDirectory;
            }

            var folder = Path.Combine(baseFolder, "ScheduleCollege");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static string DataDirectory => EnsureDirectory(Path.Combine(AppDataRoot, "Data"));
    public static string BackupsDirectory => EnsureDirectory(Path.Combine(AppDataRoot, "Backups"));

    // Статика для EXE-режима. Файлы создаются в AppData, а не рядом с EXE.
    public static string WebRootDirectory => EnsureDirectory(Path.Combine(AppDataRoot, "WebRoot"));

    public static string NormalizeSqliteConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=schedulecollege.db";
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            dataSource = "schedulecollege.db";
        }

        if (dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(dataSource))
        {
            return builder.ToString();
        }

        builder.DataSource = Path.Combine(DataDirectory, dataSource);
        return builder.ToString();
    }

    private static string EnsureDirectory(string folder)
    {
        Directory.CreateDirectory(folder);
        return folder;
    }
}
