using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using ScheduleCollege.Web.Data;

namespace ScheduleCollege.Web.Services;

public class AppDbContextFactory
{
    public AppDbContext Create(DatabaseProfile profile)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        Configure(builder, profile);
        return new AppDbContext(builder.Options);
    }

    public static void Configure(DbContextOptionsBuilder options, DatabaseProfile profile)
    {
        if (profile.Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            options.UseNpgsql(profile.ConnectionString);
            return;
        }

        if (profile.Provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
        {
            // Версия указана явно, чтобы приложение не пыталось определять её при каждом запуске контекста.
            options.UseMySql(profile.ConnectionString, new MySqlServerVersion(new Version(8, 0, 33)));
            return;
        }

        if (profile.Provider.Equals("MariaDB", StringComparison.OrdinalIgnoreCase))
        {
            // Подходит для MariaDB 10.4+ и учебной демонстрации. При необходимости версию можно поменять здесь.
            options.UseMySql(profile.ConnectionString, new MariaDbServerVersion(new Version(10, 4, 0)));
            return;
        }

        // По умолчанию используем SQLite: это самый простой режим для запуска и защиты.
        // Относительный путь к файлу БД переносим в AppData, чтобы рядом с EXE не появлялись рабочие файлы.
        options.UseSqlite(AppPaths.NormalizeSqliteConnectionString(profile.ConnectionString));
    }
}
