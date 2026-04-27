using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScheduleCollege.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccessModel : PageModel
{
    private const int Port = 5000;

    public string ComputerName { get; set; } = Environment.MachineName;
    public string LocalIp { get; set; } = "127.0.0.1";
    public string LocalUrl { get; set; } = $"http://localhost:{Port}";
    public string NetworkUrl { get; set; } = "";
    public string LocalMaskUrl { get; set; } = $"http://schedulecollege.local:{Port}";
    public string HostsLine { get; set; } = "";
    public string FirewallCommand { get; set; } = "";
    public string CaddyConfig { get; set; } = "";
    public string NginxConfig { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string ExternalDomain { get; set; } = "schedulecollege.example.ru";

    public void OnGet()
    {
        Load();
    }

    private void Load()
    {
        LocalIp = GetLocalIp();
        NetworkUrl = $"http://{LocalIp}:{Port}";
        HostsLine = $"{LocalIp} schedulecollege.local";
        FirewallCommand = "netsh advfirewall firewall add rule name=\"ScheduleCollege 5000\" dir=in action=allow protocol=TCP localport=5000";
        CaddyConfig = BuildCaddyConfig(ExternalDomain);
        NginxConfig = BuildNginxConfig(ExternalDomain);
    }

    private static string BuildCaddyConfig(string domain)
    {
        domain = NormalizeDomain(domain);

        return domain + " {\r\n" +
               $"    reverse_proxy 127.0.0.1:{Port}\r\n" +
               "}\r\n";
    }

    private static string BuildNginxConfig(string domain)
    {
        domain = NormalizeDomain(domain);

        return "server {\r\n" +
               "    listen 80;\r\n" +
               $"    server_name {domain};\r\n\r\n" +
               "    location / {\r\n" +
               $"        proxy_pass http://127.0.0.1:{Port};\r\n" +
               "        proxy_http_version 1.1;\r\n" +
               "        proxy_set_header Host $host;\r\n" +
               "        proxy_set_header X-Real-IP $remote_addr;\r\n" +
               "        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\r\n" +
               "        proxy_set_header X-Forwarded-Proto $scheme;\r\n" +
               "    }\r\n" +
               "}\r\n";
    }

    private static string NormalizeDomain(string value)
    {
        value = (value ?? "").Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return "schedulecollege.example.ru";
        }

        value = value.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
                     .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                     .Trim('/');

        return value;
    }

    private static string GetLocalIp()
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
}
