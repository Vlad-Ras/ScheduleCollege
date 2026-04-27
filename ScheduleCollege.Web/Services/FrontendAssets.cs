namespace ScheduleCollege.Web.Services;

public static class FrontendAssets
{
    public static void Ensure()
    {
        Directory.CreateDirectory(AppPaths.WebRootDirectory);

        var cssDirectory = Path.Combine(AppPaths.WebRootDirectory, "css");
        Directory.CreateDirectory(cssDirectory);
        File.WriteAllText(Path.Combine(cssDirectory, "site.css"), SiteCss);

        var jsDirectory = Path.Combine(AppPaths.WebRootDirectory, "js");
        Directory.CreateDirectory(jsDirectory);
        var jsPath = Path.Combine(jsDirectory, "site.js");
        if (!File.Exists(jsPath))
        {
            File.WriteAllText(jsPath, "// Runtime asset created by ScheduleCollege.\n");
        }
    }

    public const string SiteCss = """
:root {
    --bg: #f4f7fb;
    --card: #ffffff;
    --text: #1d2733;
    --muted: #667085;
    --primary: #2563eb;
    --primary-dark: #1d4ed8;
    --danger: #dc2626;
    --border: #d9e2ef;
    --success-bg: #ecfdf3;
    --success-text: #067647;
    --warning-bg: #fff7ed;
    --warning-text: #b45309;
}

* { box-sizing: border-box; }

body {
    margin: 0;
    font-family: Segoe UI, Arial, sans-serif;
    background: var(--bg);
    color: var(--text);
}

.topbar {
    display: flex;
    align-items: center;
    gap: 18px;
    padding: 14px 28px;
    background: #10243f;
    color: white;
    box-shadow: 0 2px 12px rgba(0,0,0,.15);
}

.brand {
    font-size: 20px;
    font-weight: 700;
    color: white;
    text-decoration: none;
    white-space: nowrap;
}

.topbar nav {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
    flex: 1;
}

.topbar a {
    color: #dbeafe;
    text-decoration: none;
}

.topbar a:hover {
    color: white;
}

.userbox {
    display: flex;
    gap: 12px;
    align-items: center;
    white-space: nowrap;
}

.container {
    width: min(1180px, calc(100% - 32px));
    margin: 28px auto;
}

.card {
    background: var(--card);
    border: 1px solid var(--border);
    border-radius: 16px;
    padding: 20px;
    box-shadow: 0 8px 24px rgba(16, 36, 63, .08);
    margin-bottom: 18px;
}

.grid {
    display: grid;
    gap: 16px;
}

.grid-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
.grid-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.grid-4 { grid-template-columns: repeat(4, minmax(0, 1fr)); }

@media (max-width: 900px) {
    .grid-2, .grid-3, .grid-4 { grid-template-columns: 1fr; }
    .topbar { align-items: flex-start; flex-direction: column; }
}

h1, h2, h3 {
    margin-top: 0;
}

.muted {
    color: var(--muted);
}

.stat {
    font-size: 34px;
    font-weight: 800;
    color: var(--primary);
}

.btn {
    display: inline-block;
    border: 0;
    border-radius: 10px;
    padding: 9px 14px;
    background: var(--primary);
    color: white;
    text-decoration: none;
    cursor: pointer;
    font-size: 14px;
}

.btn:hover {
    background: var(--primary-dark);
}

.btn.secondary {
    background: #e2e8f0;
    color: #0f172a;
}

.btn.danger {
    background: var(--danger);
}

.btn.small {
    padding: 6px 10px;
    font-size: 13px;
}

.form-row {
    display: grid;
    gap: 10px;
    margin-bottom: 12px;
}

label {
    font-weight: 600;
}

input, select, textarea {
    width: 100%;
    padding: 9px 11px;
    border: 1px solid var(--border);
    border-radius: 10px;
    font: inherit;
    background: white;
}

textarea {
    min-height: 80px;
}

table {
    width: 100%;
    border-collapse: collapse;
    background: white;
}

th, td {
    padding: 10px 12px;
    border-bottom: 1px solid var(--border);
    text-align: left;
    vertical-align: top;
}

th {
    background: #f8fafc;
    font-weight: 700;
}

.alert {
    border-radius: 12px;
    padding: 12px 14px;
    margin-bottom: 16px;
}

.alert.success {
    background: var(--success-bg);
    color: var(--success-text);
}

.alert.warning {
    background: var(--warning-bg);
    color: var(--warning-text);
}

.alert.error {
    background: #fef2f2;
    color: #b91c1c;
}

.actions {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.badge {
    display: inline-block;
    padding: 4px 8px;
    border-radius: 999px;
    background: #e0f2fe;
    color: #075985;
    font-size: 12px;
    font-weight: 700;
}


pre {
    overflow-x: auto;
    padding: 12px;
    border: 1px solid var(--border);
    border-radius: 12px;
    background: #f8fafc;
}

code {
    font-family: Consolas, Monaco, monospace;
    font-size: 14px;
}

.copy-row {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
}

.copy-row code {
    display: inline-block;
    padding: 8px 10px;
    border: 1px solid var(--border);
    border-radius: 10px;
    background: #f8fafc;
}

.inline-form {
    margin-bottom: 18px;
}


/* Schedule calendar styles are duplicated here so they are available even when Razor page-level styles are not loaded from a published EXE. */
.calendar-wrap {
    overflow-x: auto;
    border-radius: 14px;
    border: 1px solid var(--border);
}

.calendar-table {
    width: 100%;
    border-collapse: collapse;
    min-width: 1000px;
}

.calendar-table th,
.calendar-table td {
    border: 1px solid #dbe4ef;
    padding: 8px;
    vertical-align: top;
}

.calendar-table th {
    background: #f1f5f9;
    text-align: center;
}

.calendar-table td {
    min-width: 140px;
}

.lesson-card {
    background: #eef6ff;
    border: 1px solid #bfdbfe;
    border-radius: 10px;
    padding: 7px;
    margin-bottom: 6px;
    font-size: 13px;
    line-height: 1.35;
}

form.grid button,
form.grid a.btn {
    align-self: end;
}

.actions form {
    margin: 0;
}

""";
}
