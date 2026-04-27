ScheduleCollege — исправление EXE и форматирования

Что исправлено:
1. Убран вызов смены WebRoot после создания WebApplicationBuilder.
   Именно он давал ошибку:
   "The web root changed ... Use WebApplication.CreateBuilder(WebApplicationOptions) instead".
2. Глобальные CSS-стили продублированы прямо в Pages/Shared/_Layout.cshtml.
   Поэтому оформление работает и в dev-среде, и при запуске одного EXE, даже если рядом нет wwwroot.
3. Рабочие файлы приложения вынесены из папки EXE:
   %LOCALAPPDATA%\ScheduleCollege\Data\schedulecollege.db
   %LOCALAPPDATA%\ScheduleCollege\Data\database-profile.json
   %LOCALAPPDATA%\ScheduleCollege\Backups
4. Старый schedulecollege.db рядом с EXE/проектом автоматически копируется в AppData при первом запуске, если новой базы там ещё нет.

Сборка одного EXE:
1. Открой PowerShell в папке ScheduleCollege.Web.
2. Выполни:
   powershell -ExecutionPolicy Bypass -File .\build-single-exe.ps1
3. Готовый файл будет тут:
   .\publish-single\ScheduleCollege.Web.exe

Если браузер уже открыл страницу без оформления, нажми Ctrl+F5.
