using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using ScheduleCollege.Web.Data;
using ScheduleCollege.Web.Services;
using System.Windows.Forms;

QuestPDF.Settings.License = LicenseType.Community;

const int Port = 5000;

var builder = WebApplication.CreateBuilder(args);

// Не меняем WebRoot после создания WebApplicationBuilder.
// В .NET 8 это может падать ошибкой:
// "The web root changed ... Use WebApplication.CreateBuilder(WebApplicationOptions) instead".
// Стили продублированы прямо в _Layout.cshtml, поэтому оформление не зависит от wwwroot при запуске одного EXE.
MigrateLegacyRuntimeFiles(builder.Environment.ContentRootPath);

// Слушаем все сетевые интерфейсы, чтобы сайт открывался с других ПК в локальной сети.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(Port);
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Error");
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<DatabaseProfileStore>();
builder.Services.AddSingleton<AppDbContextFactory>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<DatabaseTransferService>();
builder.Services.AddScoped<ReportExportService>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var profileStore = serviceProvider.GetRequiredService<DatabaseProfileStore>();
    var profile = profileStore.GetCurrentProfile();
    AppDbContextFactory.Configure(options, profile);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SchemaUpdater.Update(db);
    SeedData.Initialize(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

if (OperatingSystem.IsWindows() && !args.Contains("--no-tray"))
{
    var serverTask = app.RunAsync();
    RunTray(app);
    await serverTask;
}
else
{
    await app.RunAsync();
}

static void RunTray(WebApplication app)
{
    ApplicationConfiguration.Initialize();

    using var icon = new NotifyIcon();
    icon.Icon = System.Drawing.SystemIcons.Application;
    icon.Text = "ScheduleCollege";
    icon.Visible = true;

    var menu = new ContextMenuStrip();
    menu.Items.Add("Открыть сайт", null, (_, _) => Open(LocalUrl()));
    menu.Items.Add("Открыть адрес в сети", null, (_, _) => Open(NetworkUrl()));
    menu.Items.Add("Учебные дни групп", null, (_, _) => Open($"{LocalUrl()}/Schedule/StudyDays"));
    menu.Items.Add("Настройки доступа", null, (_, _) => Open($"{LocalUrl()}/Admin/Access"));
    menu.Items.Add("Показать адреса", null, (_, _) => ShowAddresses());
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add("Выход", null, async (_, _) =>
    {
        icon.Visible = false;
        await app.StopAsync();
        System.Windows.Forms.Application.Exit();
    });

    icon.ContextMenuStrip = menu;
    icon.DoubleClick += (_, _) => Open(LocalUrl());

    Open(LocalUrl());
    System.Windows.Forms.Application.Run();
}

static string LocalUrl()
{
    return $"http://localhost:{Port}";
}

static string NetworkUrl()
{
    return $"http://{GetLocalIp()}:{Port}";
}

static void Open(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch
    {
        MessageBox.Show($"Не удалось открыть адрес: {url}", "ScheduleCollege");
    }
}

static void ShowAddresses()
{
    var text =
        $"На этом компьютере:\n{LocalUrl()}\n\n" +
        $"В локальной сети:\n{NetworkUrl()}\n\n" +
        "Красивая локальная маска:\nhttp://schedulecollege.local:5000\n\n" +
        "Настройка адресов доступна в админке:\n" +
        $"{LocalUrl()}/Admin/Access";

    MessageBox.Show(text, "Адреса ScheduleCollege");
}

static string GetLocalIp()
{
    try
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ip = host.AddressList.FirstOrDefault(x =>
            x.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.IsLoopback(x)
            && !x.ToString().StartsWith("169.254."));

        return ip?.ToString() ?? "127.0.0.1";
    }
    catch
    {
        return "127.0.0.1";
    }
}

static void MigrateLegacyRuntimeFiles(string contentRootPath)
{
    try
    {
        var legacyDbPath = Path.Combine(contentRootPath, "schedulecollege.db");
        var newDbPath = Path.Combine(AppPaths.DataDirectory, "schedulecollege.db");

        if (!File.Exists(newDbPath) && File.Exists(legacyDbPath))
        {
            File.Copy(legacyDbPath, newDbPath);
        }
    }
    catch
    {
        // Если перенос старой локальной базы не удался, приложение всё равно запустится
        // и создаст новую SQLite-базу в AppData.
    }
}
